namespace LeakTestWorker.MQTT.Interfaces;

public interface IMqttClientService : IMqttPublisher
{
    void Configure(string brokerHost, int brokerPort);

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
