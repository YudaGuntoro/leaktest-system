using System.Data.Common;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace LeakTestWorker.Helper;

public static class DbRetry
{
    public static async Task OpenWithRetryAsync(
        DbConnection connection,
        ILogger logger,
        string operationName,
        CancellationToken cancellationToken,
        int maxRetry = 10)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        for (var attempt = 1; attempt <= maxRetry; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex) when (attempt < maxRetry)
            {
                logger.LogWarning(ex, "[DB] {Operation} retry {Attempt}/{MaxRetry}", operationName, attempt, maxRetry);
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
            catch (MySqlConnector.MySqlException ex) when (attempt < maxRetry)
            {
                logger.LogWarning(ex, "[DB] {Operation} retry {Attempt}/{MaxRetry}", operationName, attempt, maxRetry);
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
            catch (TimeoutException ex) when (attempt < maxRetry)
            {
                logger.LogWarning(ex, "[DB] {Operation} timeout retry {Attempt}/{MaxRetry}", operationName, attempt, maxRetry);
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
        }

        await connection.OpenAsync(cancellationToken);
    }

    private static TimeSpan GetRetryDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(attempt, 10));

    public static bool IsDatabaseException(Exception exception) =>
        exception is MySql.Data.MySqlClient.MySqlException or MySqlConnector.MySqlException or TimeoutException or SocketException or IOException
        || exception.InnerException is not null && IsDatabaseException(exception.InnerException);
}
