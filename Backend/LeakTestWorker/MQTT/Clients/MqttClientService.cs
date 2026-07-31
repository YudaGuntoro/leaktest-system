using System.Net.Sockets;
using System.Text;
using LeakTestWorker.MQTT.Interfaces;
using LeakTestWorker.Singletone;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace LeakTestWorker.MQTT.Clients;

public sealed class MqttClientService : BackgroundService, IMqttClientService
{
    private const string DefaultBrokerAddress = "127.0.0.1";
    private const string DefaultClientId = "LeakTestWorker";
    private const string DefaultTopic = "/LeakTest";
    private const int DefaultPort = 1883;

    private readonly ILogger<MqttClientService> _logger;
    private readonly ILeakTestMessageHandler _messageHandler;
    private readonly IMqttClient _mqttClient;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly string _topic;
    private readonly string _clientId;
    private readonly string? _username;
    private readonly string? _password;
    private readonly int _qos;
    private readonly TimeSpan _retryDelay;
    private readonly TimeSpan _loopInterval;

    private MqttClientOptions? _mqttClientOptions;
    private string _brokerAddress;
    private int _port;
    private volatile bool _isSubscribed;

    public MqttClientService(
        ILogger<MqttClientService> logger,
        ILeakTestMessageHandler messageHandler)
    {
        _logger = logger;
        _messageHandler = messageHandler;

        var cfg = Config.Instance;
        _brokerAddress = ReadSetting(cfg, "MQTT", "Host", "MQTT__Host", "MQTT_HOST") ?? DefaultBrokerAddress;
        _port = ReadIntSetting(cfg, "MQTT", "Port", DefaultPort, "MQTT__Port", "MQTT_PORT");
        _clientId = ReadSetting(cfg, "MQTT", "ClientId", "MQTT__ClientId", "MQTT_CLIENT_ID") ?? DefaultClientId;
        _username = ReadSetting(cfg, "MQTT", "Username", "MQTT__Username", "MQTT_USERNAME");
        _password = ReadSetting(cfg, "MQTT", "Password", "MQTT__Password", "MQTT_PASSWORD");
        _qos = ReadIntSetting(cfg, "MQTT", "Qos", 1, "MQTT__Qos", "MQTT_QOS");
        _retryDelay = TimeSpan.FromSeconds(Math.Max(1, ReadIntSetting(
            cfg,
            "Worker",
            "ReconnectDelaySeconds",
            3,
            "Worker__ReconnectDelaySeconds",
            "WORKER_RECONNECT_DELAY_SECONDS")));
        _loopInterval = TimeSpan.FromSeconds(Math.Max(1, ReadIntSetting(
            cfg,
            "Worker",
            "IntervalSeconds",
            1,
            "Worker__IntervalSeconds",
            "WORKER_INTERVAL_SECONDS")));
        _topic = ReadTopic(cfg);

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        _mqttClient.ConnectedAsync += e =>
        {
            _logger.LogInformation("[MQTT] Connected. ResultCode={ResultCode}", e.ConnectResult?.ResultCode);
            _isSubscribed = false;
            return Task.CompletedTask;
        };

        _mqttClient.DisconnectedAsync += e =>
        {
            _logger.LogWarning(
                "[MQTT] Disconnected. Reason={Reason} ReasonString={ReasonString} Ex={Exception}",
                e.Reason,
                e.ReasonString,
                e.Exception?.Message);
            _isSubscribed = false;
            return Task.CompletedTask;
        };
    }

    public void Configure(string brokerHost, int brokerPort)
    {
        var host = string.IsNullOrWhiteSpace(brokerHost) ? DefaultBrokerAddress : brokerHost;
        var port = brokerPort > 0 ? brokerPort : DefaultPort;
        var clientId = string.IsNullOrWhiteSpace(_clientId)
            ? $"{DefaultClientId}-{Environment.MachineName}-{Guid.NewGuid():N}"
            : _clientId;

        var builder = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(host, port)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(10));

        if (!string.IsNullOrWhiteSpace(_username))
        {
            builder.WithCredentials(_username, _password ?? string.Empty);
        }

        _mqttClientOptions = builder.Build();

        _logger.LogInformation(
            "[MQTT] Config: Host={Host} Port={Port} Topic={Topic} ClientId={ClientId}",
            host,
            port,
            _topic,
            clientId);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_mqttClientOptions is null)
        {
            throw new InvalidOperationException("MQTT client options are not configured.");
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_mqttClient.IsConnected)
            {
                return;
            }

            _logger.LogInformation("[MQTT] Connecting...");
            await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);

            if (_mqttClient.IsConnected)
            {
                _logger.LogInformation("[MQTT] Connected OK.");
            }
            else
            {
                _logger.LogWarning("[MQTT] Connect finished but client is still disconnected.");
            }
        }
        catch (MqttCommunicationException ex)
        {
            _logger.LogError(ex, "[MQTT] Communication error.");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "[MQTT] Socket error.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Connect exception.");
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (!_mqttClient.IsConnected)
        {
            throw new InvalidOperationException("MQTT client is not connected.");
        }

        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter
                .WithTopic(topic)
                .WithQualityOfServiceLevel(ToQualityOfServiceLevel(_qos)))
            .Build();

        await _mqttClient.SubscribeAsync(options, cancellationToken);
        _logger.LogInformation("[MQTT] Subscribed: {Topic}", topic);
    }

    public async Task PublishAsync(
        string topic,
        string payload,
        bool retain = false,
        int qos = 0,
        CancellationToken cancellationToken = default)
    {
        if (!_mqttClient.IsConnected)
        {
            throw new InvalidOperationException("MQTT client is not connected.");
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload ?? string.Empty)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(ToQualityOfServiceLevel(qos))
            .Build();

        await _mqttClient.PublishAsync(message, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_brokerAddress))
        {
            _brokerAddress = DefaultBrokerAddress;
        }

        if (_port <= 0)
        {
            _port = DefaultPort;
        }

        Configure(_brokerAddress, _port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await ConnectAsync(stoppingToken);

                    if (!_mqttClient.IsConnected)
                    {
                        await _messageHandler.ReprocessBufferAsync(stoppingToken);
                        await Task.Delay(_retryDelay, stoppingToken);
                        continue;
                    }
                }

                await SubscribeConfiguredTopicAsync(stoppingToken);
                await _messageHandler.ReprocessBufferAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MQTT] Loop error, retry.");
                await Task.Delay(_retryDelay, stoppingToken);
            }

            await Task.Delay(_loopInterval, stoppingToken);
        }
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage?.Topic ?? string.Empty;
        var payloadText = GetPayloadText(e.ApplicationMessage);

        try
        {
            await _messageHandler.HandleAsync(topic, payloadText, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Failed to save message to history. Topic={Topic}", topic);
        }
    }

    private async Task SubscribeConfiguredTopicAsync(CancellationToken cancellationToken)
    {
        if (!_mqttClient.IsConnected || _isSubscribed)
        {
            return;
        }

        try
        {
            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter
                    .WithTopic(_topic)
                    .WithQualityOfServiceLevel(ToQualityOfServiceLevel(_qos)))
                .Build();

            await _mqttClient.SubscribeAsync(options, cancellationToken);
            _isSubscribed = true;

            _logger.LogInformation("[MQTT] Subscribed topic: {Topic}", _topic);
        }
        catch (Exception ex)
        {
            _isSubscribed = false;
            _logger.LogWarning(ex, "[MQTT] Subscribe failed, retrying.");
        }
    }

    private static string GetPayloadText(MqttApplicationMessage? message)
    {
        if (message is null)
        {
            return string.Empty;
        }

        var segment = message.PayloadSegment;
        if (segment.Array is null || segment.Count == 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
    }

    private static string ReadTopic(Config cfg)
    {
        return ReadSetting(cfg, "MQTT", "Topic", "MQTT__Topic", "MQTT_TOPIC") ?? DefaultTopic;
    }

    private static string? ReadSetting(Config cfg, string section, string key, params string[] environmentKeys)
    {
        foreach (var environmentKey in environmentKeys)
        {
            var value = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return cfg.Read(key, section);
    }

    private static int ReadIntSetting(
        Config cfg,
        string section,
        string key,
        int defaultValue,
        params string[] environmentKeys)
    {
        var value = ReadSetting(cfg, section, key, environmentKeys);
        return int.TryParse(value, out var parsed) ? parsed : cfg.ReadInt(key, section, defaultValue);
    }

    private static MqttQualityOfServiceLevel ToQualityOfServiceLevel(int qos) =>
        qos switch
        {
            1 => MqttQualityOfServiceLevel.AtLeastOnce,
            2 => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MqttQualityOfServiceLevel.AtMostOnce
        };
}
