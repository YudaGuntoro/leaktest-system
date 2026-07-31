namespace LeakTestWorker.MQTT.Interfaces;

public interface ILeakTestMachineHandler
{
    string MachineName { get; }

    string Topic { get; }

    bool CanHandle(string topic);

    Task<long> InsertAsync(string topic, string payload, CancellationToken cancellationToken = default);
}
