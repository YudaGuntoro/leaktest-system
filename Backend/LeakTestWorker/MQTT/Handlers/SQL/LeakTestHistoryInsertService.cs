using Dapper;
using LeakTestWorker.Domains.Models;
using LeakTestWorker.Helper;
using LeakTestWorker.Singletone;
using MySql.Data.MySqlClient;

namespace LeakTestWorker.MQTT.Handlers.SQL;

public sealed class LeakTestHistoryInsertService : ILeakTestHistoryInsertService
{
    private readonly ILogger<LeakTestHistoryInsertService> _logger;

    public LeakTestHistoryInsertService(ILogger<LeakTestHistoryInsertService> logger)
    {
        _logger = logger;
    }

    public async Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(dbConfig.MysqlConnString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "DB_WARMUP", cancellationToken, maxRetry: 60);
        _logger.LogInformation("[DB] READY");
    }

    public async Task<long> InsertAsync(LeakTestHistoryRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(dbConfig.MysqlConnString);
        await DbRetry.OpenWithRetryAsync(connection, _logger, "INSERT_HISTORY", cancellationToken);
        await EnsureHmiColumnsAsync(connection, cancellationToken);
        await EnsureJudgementMasterAsync(connection, cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            var engineModelId = await ResolveEngineModelIdAsync(connection, transaction, record, cancellationToken);
            var result = await ResolveJudgementResultAsync(connection, transaction, record, cancellationToken);
            var barcodeScan = FirstText(record.BarcodeScan, BuildBarcodeScan(record.EngineModel, record.EngineNumber));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO leak_test_work_records
                    (engine_model_id, engine_number, barcode_scan, check_date, check_time, machine_name, operator_name, parameter_pressure, channel_no, press_set_up, press_set_low, pressure_input, cycle_time_leak_test_minutes, result, created_at, updated_at)
                VALUES
                    (@engine_model_id, @engine_number, @barcode_scan, @check_date, @check_time, @machine_name, @operator_name, @parameter_pressure, @channel_no, @press_set_up, @press_set_low, @pressure_input, @cycle_time, @result, NOW(), NOW());
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
                    result
                },
                transaction,
                cancellationToken: cancellationToken));

            var insertedId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                "SELECT LAST_INSERT_ID();",
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return insertedId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

    private static async Task<string> ResolveJudgementResultAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        LeakTestHistoryRecord record,
        CancellationToken cancellationToken)
    {
        if (record.JudgementCode.HasValue)
        {
            var masterResult = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                """
                SELECT result
                FROM leak_test_judgements
                WHERE judgement_code = @judgement_code
                  AND is_deleted <> 1
                LIMIT 1;
                """,
                new { judgement_code = record.JudgementCode.Value },
                transaction,
                cancellationToken: cancellationToken));

            if (masterResult is "OK" or "NG")
            {
                return masterResult;
            }
        }

        return record.Result;
    }

    private static async Task EnsureHmiColumnsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(connection, "barcode_scan", "ALTER TABLE leak_test_work_records ADD COLUMN barcode_scan VARCHAR(180) NULL AFTER engine_number", cancellationToken);
        await EnsureColumnAsync(connection, "channel_no", "ALTER TABLE leak_test_work_records ADD COLUMN channel_no VARCHAR(20) NULL AFTER parameter_pressure", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_up", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_up DECIMAL(8, 2) NULL AFTER channel_no", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_low", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_low DECIMAL(8, 2) NULL AFTER press_set_up", cancellationToken);
        await EnsureColumnAsync(connection, "operator_name", "ALTER TABLE leak_test_work_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER machine_name", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_barcode_scan", "CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_channel_no", "CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)", cancellationToken);
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
                (16, '', '', '', 0)
            ON DUPLICATE KEY UPDATE
                result = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(result), result),
                note = IF(is_deleted = 1 OR note LIKE 'Temporary dummy%' OR note IN ('Gateway judgement OK', 'Gateway judgement NG'), VALUES(note), note),
                is_deleted = VALUES(is_deleted),
                judgement_name = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(judgement_name), judgement_name),
                updated_at = CURRENT_TIMESTAMP;
            """,
            cancellationToken: cancellationToken));

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
        var model = engineModel?.Trim();
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
}
