using LeakTestWorker.Domains.Models;

namespace LeakTestWorker.MQTT.Handlers.SQL;

public interface ILeakTestHistoryInsertService
{
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    Task<long> InsertAsync(LeakTestHistoryRecord record, CancellationToken cancellationToken = default);
}
