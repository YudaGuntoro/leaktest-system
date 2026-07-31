namespace LeakTestWorker.MQTT.Interfaces;

public interface ILeakTestMessageHandler
{
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    Task HandleAsync(string topic, string payload, CancellationToken cancellationToken = default);

    Task ReprocessBufferAsync(CancellationToken cancellationToken = default);
}
