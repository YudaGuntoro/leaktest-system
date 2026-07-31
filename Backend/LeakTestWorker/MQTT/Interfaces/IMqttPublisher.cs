namespace LeakTestWorker.MQTT.Interfaces;

public interface IMqttPublisher
{
    Task PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        int qos = 0,
        CancellationToken cancellationToken = default);
}
