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

        using var transaction = connection.BeginTransaction();
        try
        {
            var engineModelId = await ResolveEngineModelIdAsync(connection, transaction, record, cancellationToken);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO leak_test_work_records
                    (engine_model_id, engine_number, check_date, check_time, machine_name, parameter_pressure, pressure_input, cycle_time_leak_test_minutes, result, created_at, updated_at)
                VALUES
                    (@engine_model_id, @engine_number, @check_date, @check_time, @machine_name, @parameter_pressure, @pressure_input, @cycle_time, @result, NOW(), NOW());
                """;
            command.Parameters.AddWithValue("@engine_model_id", engineModelId);
            command.Parameters.AddWithValue("@engine_number", Clamp(record.EngineNumber, 120));
            command.Parameters.AddWithValue("@check_date", record.CheckDate.Date);
            command.Parameters.AddWithValue("@check_time", Clamp(record.CheckTime, 8));
            command.Parameters.AddWithValue("@machine_name", Clamp(record.MachineName, 150));
            command.Parameters.AddWithValue("@parameter_pressure", record.ParameterPressure);
            command.Parameters.AddWithValue("@pressure_input", record.PressureInput);
            command.Parameters.AddWithValue("@cycle_time", record.CycleTimeLeakTestMinutes);
            command.Parameters.AddWithValue("@result", record.Result);

            await command.ExecuteNonQueryAsync(cancellationToken);
            var insertedId = command.LastInsertedId;

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
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id FROM engine_models WHERE id = @id LIMIT 1;";
        command.Parameters.AddWithValue("@id", engineModelId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return ToNullableInt(result);
    }

    private static async Task<int> FindOrCreateEngineModelAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string engineModel,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO engine_models (engine_model, description, note, is_deleted)
            VALUES (@engine_model, 'MQTT', 'Created by LeakTestWorker', 0)
            ON DUPLICATE KEY UPDATE id = LAST_INSERT_ID(id);
            """;
        command.Parameters.AddWithValue("@engine_model", Clamp(engineModel, 45));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static int? ToNullableInt(object? value)
    {
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    private static string Clamp(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
