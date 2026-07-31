using LeakTestWorker.Domains.Models;

namespace LeakTestWorker.MQTT.Interfaces;

public interface ILeakTestMessageBuffer
{
    Task EnqueueAsync(BufferedLeakTestMessage message, CancellationToken cancellationToken = default);

    Task<int> ProcessAsync(
        Func<BufferedLeakTestMessage, CancellationToken, Task<bool>> processor,
        int maxItems,
        CancellationToken cancellationToken = default);
}
