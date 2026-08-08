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

        using var transaction = connection.BeginTransaction();
        try
        {
            var engineModelId = await ResolveEngineModelIdAsync(connection, transaction, record, cancellationToken);
            var operatorId = await ResolveOperatorIdAsync(connection, transaction, record, cancellationToken);
            var barcodeScan = FirstText(record.BarcodeScan, BuildBarcodeScan(record.EngineModel, record.EngineNumber));

            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO leak_test_work_records
                    (engine_model_id, engine_number, barcode_scan, check_date, check_time, machine_name, operator_id, parameter_pressure, channel_no, press_set_up, press_set_low, pressure_input, cycle_time_leak_test_minutes, result, created_at, updated_at)
                VALUES
                    (@engine_model_id, @engine_number, @barcode_scan, @check_date, @check_time, @machine_name, @operator_id, @parameter_pressure, @channel_no, @press_set_up, @press_set_low, @pressure_input, @cycle_time, @result, NOW(), NOW());
                """,
                new
                {
                    engine_model_id = engineModelId,
                    engine_number = Clamp(record.EngineNumber, 120),
                    barcode_scan = DbText(barcodeScan, 180),
                    check_date = record.CheckDate.Date,
                    check_time = Clamp(record.CheckTime, 8),
                    machine_name = Clamp(record.MachineName, 150),
                    operator_id = operatorId,
                    parameter_pressure = record.ParameterPressure,
                    channel_no = DbText(record.ChannelNo, 20),
                    press_set_up = record.PressSetUp,
                    press_set_low = record.PressSetLow,
                    pressure_input = record.PressureInput,
                    cycle_time = record.CycleTimeLeakTestMinutes,
                    result = record.Result
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

    private async Task<int?> ResolveOperatorIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        LeakTestHistoryRecord record,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.Operator))
        {
            return null;
        }

        var existingId = await FindOperatorAsync(connection, transaction, record.Operator, cancellationToken);
        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        return await FindOrCreateOperatorAsync(connection, transaction, record.Operator, cancellationToken);
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

    private static async Task<int?> FindOperatorAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string operatorText,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            """
            SELECT id
            FROM operators
            WHERE operator_code = @operator OR operator_name = @operator
            LIMIT 1;
            """,
            new { @operator = operatorText.Trim() },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<int> FindOrCreateOperatorAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string operatorText,
        CancellationToken cancellationToken)
    {
        var operatorCode = await BuildUniqueOperatorCodeAsync(connection, transaction, operatorText, cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO operators (operator_code, operator_name, department, note, is_deleted, created_at, updated_at)
            VALUES (@operator_code, @operator_name, 'Production', 'Created by HMI payload', 0, NOW(), NOW())
            ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id), updated_at = NOW();
            """,
            new
            {
                operator_code = operatorCode,
                operator_name = Clamp(operatorText, 150)
            },
            transaction,
            cancellationToken: cancellationToken));

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT LAST_INSERT_ID();",
            transaction: transaction,
            cancellationToken: cancellationToken));
    }

    private static async Task<string> BuildUniqueOperatorCodeAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string operatorText,
        CancellationToken cancellationToken)
    {
        var alphanumeric = new string(operatorText
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        var baseCode = Clamp($"HMI-{(string.IsNullOrWhiteSpace(alphanumeric) ? "OPERATOR" : alphanumeric)}", 50);
        var code = baseCode;
        var suffix = 1;

        while (await OperatorCodeExistsAsync(connection, transaction, code, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var prefixLength = Math.Min(baseCode.Length, 50 - suffixText.Length);
            code = $"{baseCode[..prefixLength]}{suffixText}";
            suffix++;
        }

        return code;
    }

    private static async Task<bool> OperatorCodeExistsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string operatorCode,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM operators WHERE operator_code = @operator_code;",
            new { operator_code = operatorCode },
            transaction,
            cancellationToken: cancellationToken));

        return count > 0;
    }

    private static async Task EnsureHmiColumnsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(connection, "barcode_scan", "ALTER TABLE leak_test_work_records ADD COLUMN barcode_scan VARCHAR(180) NULL AFTER engine_number", cancellationToken);
        await EnsureColumnAsync(connection, "channel_no", "ALTER TABLE leak_test_work_records ADD COLUMN channel_no VARCHAR(20) NULL AFTER parameter_pressure", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_up", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_up DECIMAL(8, 2) NULL AFTER channel_no", cancellationToken);
        await EnsureColumnAsync(connection, "press_set_low", "ALTER TABLE leak_test_work_records ADD COLUMN press_set_low DECIMAL(8, 2) NULL AFTER press_set_up", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_barcode_scan", "CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)", cancellationToken);
        await EnsureIndexAsync(connection, "ix_leak_test_work_records_channel_no", "CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)", cancellationToken);
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
