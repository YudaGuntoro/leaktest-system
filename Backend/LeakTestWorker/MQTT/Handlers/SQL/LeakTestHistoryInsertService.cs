using Dapper;
using LeakTestWorker.Domains.Models;
using LeakTestWorker.Helper;
using LeakTestWorker.Singletone;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

namespace LeakTestWorker.MQTT.Handlers.SQL;

public sealed class LeakTestHistoryInsertService : ILeakTestHistoryInsertService
{
    private const int ReworkTriggerNgCount = 3;

    private readonly ILogger<LeakTestHistoryInsertService> _logger;
    private readonly string _connectionString;

    public LeakTestHistoryInsertService(ILogger<LeakTestHistoryInsertService> logger, string? connectionString = null)
    {
        _logger = logger;
        _connectionString = string.IsNullOrWhiteSpace(connectionString) ? dbConfig.MysqlConnString : connectionString;
    }

    public async Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "DB_WARMUP", cancellationToken, maxRetry: 60);
        await EnsureRejectedMessagesTableAsync(connection, cancellationToken);
        _logger.LogInformation("[DB] READY");
    }

    public async Task<long> SaveRejectedMessageAsync(
        string topic,
        string payload,
        string error,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "SAVE_REJECTED_MQTT", cancellationToken);
        await EnsureRejectedMessagesTableAsync(connection, cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO leak_test_rejected_mqtt_messages (topic, payload, error_message, status, received_at)
            VALUES (@topic, @payload, @error, 'Pending', NOW());
            """,
            new { topic, payload, error },
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT LAST_INSERT_ID();",
            cancellationToken: cancellationToken));
    }

    public async Task DeleteRejectedMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "DELETE_REPROCESSED_MQTT", cancellationToken);
        await EnsureRejectedMessagesTableAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM leak_test_rejected_mqtt_messages WHERE id = @id;",
            new { id },
            cancellationToken: cancellationToken));
    }

    private static Task DeleteRejectedMessageAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long id,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM leak_test_rejected_mqtt_messages WHERE id = @id;",
            new { id },
            transaction,
            cancellationToken: cancellationToken));

    public async Task UpdateRejectedMessageFailureAsync(long id, string error, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "UPDATE_REJECTED_MQTT", cancellationToken);
        await EnsureRejectedMessagesTableAsync(connection, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE leak_test_rejected_mqtt_messages SET error_message = @error, status = 'Pending' WHERE id = @id;",
            new { id, error },
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureRejectedMessagesTableAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CREATE TABLE IF NOT EXISTS leak_test_rejected_mqtt_messages (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                topic VARCHAR(255) NOT NULL,
                payload LONGTEXT NOT NULL,
                error_message TEXT NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'Pending',
                attempt_count INT NOT NULL DEFAULT 0,
                received_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_attempt_at DATETIME NULL,
                processed_at DATETIME NULL,
                KEY ix_rejected_mqtt_status_received (status, received_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """,
            cancellationToken: cancellationToken));
    }

    public async Task<long> InsertAsync(LeakTestHistoryRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "INSERT_HISTORY", cancellationToken);
        await EnsureRejectedMessagesTableAsync(connection, cancellationToken);
        await EnsureHmiColumnsAsync(connection, cancellationToken);
        await EnsureJudgementMasterAsync(connection, cancellationToken);
        await EnsureReworkEngineTableAsync(connection, cancellationToken);
        await EnsureColumnAsync(connection, "rejected_mqtt_message_id", "ALTER TABLE leak_test_work_records ADD COLUMN rejected_mqtt_message_id BIGINT NULL AFTER source_payload", cancellationToken);
        await EnsureIndexAsync(connection, "ux_leak_test_work_records_rejected_mqtt_message_id", "CREATE UNIQUE INDEX ux_leak_test_work_records_rejected_mqtt_message_id ON leak_test_work_records (rejected_mqtt_message_id)", cancellationToken);
        await EnsureColumnAsync(connection, "source_payload_hash", "ALTER TABLE leak_test_work_records ADD COLUMN source_payload_hash CHAR(64) NULL AFTER rejected_mqtt_message_id", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_source_payload_hash", "CREATE INDEX ix_leak_test_work_records_source_payload_hash ON leak_test_work_records (source_payload_hash)", cancellationToken);
        var unhashedPayloadCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM leak_test_work_records WHERE source_payload_hash IS NULL AND source_payload IS NOT NULL;",
            cancellationToken: cancellationToken));
        if (unhashedPayloadCount > 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE leak_test_work_records SET source_payload_hash = SHA2(source_payload, 256) WHERE source_payload_hash IS NULL AND source_payload IS NOT NULL;",
                cancellationToken: cancellationToken));
        }

        var sourcePayloadHash = string.IsNullOrEmpty(record.RawPayload)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.RawPayload)));

        using var transaction = connection.BeginTransaction();
        try
        {
            if (sourcePayloadHash is not null)
            {
                var existingPayloadId = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(
                    "SELECT id FROM leak_test_work_records WHERE source_payload_hash = @hash LIMIT 1 FOR UPDATE;",
                    new { hash = sourcePayloadHash },
                    transaction,
                    cancellationToken: cancellationToken));
                if (existingPayloadId.HasValue)
                {
                    if (record.RejectedMqttMessageId.HasValue)
                    {
                        await DeleteRejectedMessageAsync(connection, transaction, record.RejectedMqttMessageId.Value, cancellationToken);
                    }
                    await transaction.CommitAsync(cancellationToken);
                    return existingPayloadId.Value;
                }
            }

            if (record.RejectedMqttMessageId.HasValue)
            {
                var existingId = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(
                    "SELECT id FROM leak_test_work_records WHERE rejected_mqtt_message_id = @id LIMIT 1 FOR UPDATE;",
                    new { id = record.RejectedMqttMessageId.Value },
                    transaction,
                    cancellationToken: cancellationToken));
                if (existingId.HasValue)
                {
                    await DeleteRejectedMessageAsync(connection, transaction, record.RejectedMqttMessageId.Value, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return existingId.Value;
                }
            }

            var engineModelId = await ResolveEngineModelIdAsync(connection, transaction, record, cancellationToken);
            var barcodeScan = FirstText(record.BarcodeScan, BuildBarcodeScan(record.EngineModel, record.EngineNumber));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO leak_test_work_records
                    (engine_model_id, engine_number, barcode_scan, check_date, check_time, machine_name, operator_name, parameter_pressure, channel_no, press_set_up, press_set_low, pressure_input, cycle_time_leak_test_minutes, judgement_code, source_payload, rejected_mqtt_message_id, source_payload_hash, created_at, updated_at)
                VALUES
                    (@engine_model_id, @engine_number, @barcode_scan, @check_date, @check_time, @machine_name, @operator_name, @parameter_pressure, @channel_no, @press_set_up, @press_set_low, @pressure_input, @cycle_time, @judgement_code, @source_payload, @rejected_mqtt_message_id, @source_payload_hash, NOW(), NOW());
                """,
                new
                {
                    engine_model_id = engineModelId,
                    engine_number = Clamp(record.EngineNumber, 120),
                    barcode_scan = DbText(barcodeScan, 180),
                    check_date = record.CheckDate.Date,
                    check_time = Clamp(record.CheckTime, 8),
                    machine_name = Clamp(record.MachineName, 150),
                    operator_name = DbText(record.Operator, 150),
                    parameter_pressure = record.ParameterPressure,
                    channel_no = DbText(record.ChannelNo, 20),
                    press_set_up = record.PressSetUp,
                    press_set_low = record.PressSetLow,
                    pressure_input = record.PressureInput,
                    cycle_time = record.CycleTimeLeakTestMinutes,
                    judgement_code = record.JudgementCode,
                    source_payload = record.RawPayload,
                    rejected_mqtt_message_id = record.RejectedMqttMessageId,
                    source_payload_hash = sourcePayloadHash
                },
                transaction,
                cancellationToken: cancellationToken));

            var insertedId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                "SELECT LAST_INSERT_ID();",
                transaction: transaction,
                cancellationToken: cancellationToken));

            await PromoteToReworkEngineIfNeededAsync(connection, transaction, engineModelId, record, barcodeScan, cancellationToken);

            if (record.RejectedMqttMessageId.HasValue)
            {
                await DeleteRejectedMessageAsync(connection, transaction, record.RejectedMqttMessageId.Value, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return insertedId;
        }
        catch (MySqlException ex) when (ex.Number == 1062 && record.RejectedMqttMessageId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            var existingId = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(
                "SELECT id FROM leak_test_work_records WHERE rejected_mqtt_message_id = @id LIMIT 1;",
                new { id = record.RejectedMqttMessageId.Value },
                cancellationToken: cancellationToken));
            if (existingId.HasValue)
            {
                return existingId.Value;
            }
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task PromoteToReworkEngineIfNeededAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int engineModelId,
        LeakTestHistoryRecord record,
        string? barcodeScan,
        CancellationToken cancellationToken)
    {
        if (EvaluateWorkRecordResult(record.JudgementCode) != "NG")
        {
            return;
        }

        var engineNumber = Clamp(record.EngineNumber, 120);
        var duplicateExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM rework_engine_records
            WHERE (@barcode_scan IS NOT NULL AND barcode_scan = @barcode_scan)
               OR (engine_model_id = @engine_model_id AND engine_number = @engine_number);
            """,
            new
            {
                barcode_scan = DbText(barcodeScan, 180),
                engine_model_id = engineModelId,
                engine_number = engineNumber
            },
            transaction,
            cancellationToken: cancellationToken));

        if (duplicateExists > 0)
        {
            return;
        }

        var relatedRecords = await connection.QueryAsync<WorkRecordResultRow>(new CommandDefinition(
            """
            SELECT pressure_input AS PressureInput,
                   press_set_low AS PressSetLow,
                   press_set_up AS PressSetUp,
                   judgement_code AS JudgementCode
            FROM leak_test_work_records
            WHERE engine_model_id = @engine_model_id
              AND engine_number = @engine_number
            ORDER BY check_date DESC, check_time DESC, id DESC
            LIMIT 50;
            """,
            new
            {
                engine_model_id = engineModelId,
                engine_number = engineNumber
            },
            transaction,
            cancellationToken: cancellationToken));

        var ngCount = relatedRecords.Count(row =>
            EvaluateWorkRecordResult(row.JudgementCode) == "NG");

        if (ngCount < ReworkTriggerNgCount)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO rework_engine_records
                (engine_model_id, engine_number, barcode_scan, rework_date, rework_time, operator_name, parameter_pressure, pressure_input, result, note, created_at, updated_at)
            VALUES
                (@engine_model_id, @engine_number, @barcode_scan, @rework_date, @rework_time, @operator_name, @parameter_pressure, @pressure_input, 'NG', @note, NOW(), NOW());
            """,
            new
            {
                engine_model_id = engineModelId,
                engine_number = engineNumber,
                barcode_scan = DbText(FirstText(barcodeScan, BuildBarcodeScan(record.EngineModel, record.EngineNumber), record.EngineNumber) ?? record.EngineNumber, 180),
                rework_date = record.CheckDate.Date,
                rework_time = Clamp(record.CheckTime, 8),
                operator_name = DbText(record.Operator, 150),
                parameter_pressure = record.ParameterPressure,
                pressure_input = record.PressureInput,
                note = Clamp($"Auto rework after {ngCount} NG leak test records.", 255)
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static string EvaluateWorkRecordResult(int? judgementCode)
    {
        return judgementCode == 2 ? "OK" : "NG";
    }

    private async Task<int> ResolveEngineModelIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        LeakTestHistoryRecord record,
        CancellationToken cancellationToken)
    {
        if (record.EngineModelId is > 0)
        {
            var existingId = await FindEngineModelByIdAsync(connection, transaction, record.EngineModelId.Value, cancellationToken);
            if (existingId.HasValue)
            {
                return existingId.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(record.EngineModel))
        {
            throw new InvalidOperationException($"Engine model id {record.EngineModelId} was not found.");
        }

        return await FindOrCreateEngineModelAsync(connection, transaction, record.EngineModel, cancellationToken);
    }

    private static async Task<int?> FindEngineModelByIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int engineModelId,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT id FROM engine_models WHERE id = @id LIMIT 1;",
            new { id = engineModelId },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<int> FindOrCreateEngineModelAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string engineModel,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO engine_models (engine_model, description, note, is_deleted)
            VALUES (@engine_model, 'MQTT', 'Created by LeakTestWorker', 0)
            ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
            """,
            new { engine_model = Clamp(engineModel, 45) },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT LAST_INSERT_ID();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureHmiColumnsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(connection, "barcode_scan", "ALTER TABLE leak_test_work_records ADD COLUMN barcode_scan VARCHAR(180) NULL AFTER engine_number", cancellationToken);
        await EnsureColumnAsync(connection, "channel_no", "ALTER TABLE leak_test_work_records ADD COLUMN channel_no VARCHAR(20) NULL AFTER parameter_pressure", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_up", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_up DECIMAL(8, 2) NULL AFTER channel_no", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_low", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_low DECIMAL(8, 2) NULL AFTER press_set_up", cancellationToken);
        await EnsureColumnAsync(connection, "operator_name", "ALTER TABLE leak_test_work_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER machine_name", cancellationToken);
        await EnsureColumnAsync(connection, "judgement_code", "ALTER TABLE leak_test_work_records ADD COLUMN judgement_code INT NULL AFTER cycle_time_leak_test_minutes", cancellationToken);
        await EnsureColumnAsync(connection, "source_payload", "ALTER TABLE leak_test_work_records ADD COLUMN source_payload LONGTEXT NULL AFTER judgement_code", cancellationToken);
        await DropJudgementNameColumnAsync(connection, cancellationToken);
        await DropResultColumnAsync(connection, cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_barcode_scan", "CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_channel_no", "CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_judgement_code", "CREATE INDEX ix_leak_test_work_records_judgement_code ON leak_test_work_records (judgement_code)", cancellationToken);
    }

    private static async Task DropResultColumnAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await DropIndexIfExistsAsync(connection, "ix_leak_test_work_records_result", cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'leak_test_work_records'
              AND COLUMN_NAME = 'result';
            """,
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "ALTER TABLE leak_test_work_records DROP COLUMN result;",
            cancellationToken: cancellationToken));
    }

    private static async Task DropJudgementNameColumnAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'leak_test_work_records'
              AND COLUMN_NAME = 'judgement_name';
            """,
            cancellationToken: cancellationToken));

        if (exists == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "ALTER TABLE leak_test_work_records DROP COLUMN judgement_name;",
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureJudgementMasterAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CREATE TABLE IF NOT EXISTS leak_test_judgements (
                id INT AUTO_INCREMENT PRIMARY KEY,
                judgement_code INT NOT NULL,
                judgement_name VARCHAR(80) NOT NULL,
                result VARCHAR(10) NOT NULL,
                note VARCHAR(150) NULL,
                is_deleted TINYINT(1) NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                UNIQUE KEY uq_leak_test_judgements_code (judgement_code),
                KEY ix_leak_test_judgements_result (result)
            );
            """,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO leak_test_judgements
                (judgement_code, judgement_name, result, note, is_deleted)
            VALUES
                (1, 'LL NG', 'NG', 'HMI judgement', 0),
                (2, 'PASS', 'OK', 'HMI judgement', 0),
                (3, 'UL NG', 'NG', 'HMI judgement', 0),
                (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
                (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
                (6, 'ERROR', 'NG', 'HMI judgement', 0),
                (7, '', '', '', 0),
                (8, '', '', '', 0),
                (9, '', '', '', 0),
                (10, '', '', '', 0),
                (11, '', '', '', 0),
                (12, '', '', '', 0),
                (13, '', '', '', 0),
                (14, '', '', '', 0),
                (15, '', '', '', 0),
                (16, '', '', '', 0),
                (17, '', '', '', 0),
                (18, '', '', '', 0),
                (19, '', '', '', 0),
                (20, '', '', '', 0)
            ON DUPLICATE KEY UPDATE
                result = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(result), result),
                note = IF(is_deleted = 1 OR note LIKE 'Temporary dummy%' OR note IN ('Gateway judgement OK', 'Gateway judgement NG'), VALUES(note), note),
                is_deleted = VALUES(is_deleted),
                judgement_name = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(judgement_name), judgement_name),
                updated_at = CURRENT_TIMESTAMP;
            """,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE leak_test_judgements
            SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
            WHERE judgement_code > 20;
            """,
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureReworkEngineTableAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CREATE TABLE IF NOT EXISTS rework_engine_records (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                engine_model_id INT NULL,
                engine_model_text VARCHAR(80) NULL,
                engine_number VARCHAR(120) NOT NULL,
                barcode_scan VARCHAR(180) NOT NULL,
                rework_date DATE NOT NULL,
                rework_time VARCHAR(8) NOT NULL,
                operator_name VARCHAR(150) NULL,
                parameter_pressure DECIMAL(8, 2) NOT NULL,
                pressure_input DECIMAL(8, 2) NOT NULL,
                result VARCHAR(10) NOT NULL,
                note VARCHAR(255) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                KEY ix_rework_engine_records_date_engine (rework_date, engine_number),
                KEY ix_rework_engine_records_engine_model (engine_model_id),
                CONSTRAINT fk_rework_engine_records_engine_model
                    FOREIGN KEY (engine_model_id) REFERENCES engine_models (id)
                    ON DELETE SET NULL
            );
            """,
            cancellationToken: cancellationToken));

        await EnsureReworkColumnAsync(connection, "operator_name", "ALTER TABLE rework_engine_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER rework_time", cancellationToken);
        await EnsureReworkColumnAsync(connection, "note", "ALTER TABLE rework_engine_records ADD COLUMN note VARCHAR(255) NULL AFTER result", cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        MySqlConnection connection,
        string columnName,
        string alterSql,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'leak_test_work_records'
              AND COLUMN_NAME = @column_name;
            """,
            new { column_name = columnName },
            cancellationToken: cancellationToken));

        if (count > 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            alterSql,
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureIndexAsync(
        MySqlConnection connection,
        string indexName,
        string createSql,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'leak_test_work_records'
              AND INDEX_NAME = @index_name;
            """,
            new { index_name = indexName },
            cancellationToken: cancellationToken));

        if (count > 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            createSql,
            cancellationToken: cancellationToken));
    }

    private static async Task DropIndexIfExistsAsync(
        MySqlConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'leak_test_work_records'
              AND INDEX_NAME = @index_name;
            """,
            new { index_name = indexName },
            cancellationToken: cancellationToken));

        if (count == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            $"DROP INDEX {indexName} ON leak_test_work_records;",
            cancellationToken: cancellationToken));
    }

    private static async Task EnsureReworkColumnAsync(
        MySqlConnection connection,
        string columnName,
        string alterSql,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'rework_engine_records'
              AND COLUMN_NAME = @column_name;
            """,
            new { column_name = columnName },
            cancellationToken: cancellationToken));

        if (count > 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            alterSql,
            cancellationToken: cancellationToken));
    }

    private static string Clamp(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? FirstText(params string?[] values)
    {
        return values
            .Select(value => value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? BuildBarcodeScan(string? engineModel, string? serialNo)
    {
        var model = engineModel?.Trim().TrimStart('.');
        var serial = serialNo?.Trim();

        if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(serial))
        {
            return null;
        }

        return Clamp($"{model} {serial}", 180);
    }

    private static string? DbText(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Clamp(value, maxLength);
    }

    private sealed class WorkRecordResultRow
    {
        public decimal PressureInput { get; init; }

        public decimal? PressSetLow { get; init; }

        public decimal? PressSetUp { get; init; }

        public int? JudgementCode { get; init; }
    }
}
