using LeakTestWorker.Domains.Mappings;
using LeakTestWorker.MQTT.Handlers.SQL;
using LeakTestWorker.MQTT.Interfaces;
using LeakTestWorker.Singletone;

namespace LeakTestWorker.MQTT.Handlers.Service;

public abstract class LeakTestMachineHandlerBase : ILeakTestMachineHandler
{
    private readonly ILogger _logger;
    private readonly ILeakTestHistoryInsertService _historyInsertService;

    protected LeakTestMachineHandlerBase(
        string machineName,
        string topicConfigKey,
        string defaultTopic,
        ILogger logger,
        ILeakTestHistoryInsertService historyInsertService)
    {
        MachineName = machineName;
        Topic = Config.Instance.Read(topicConfigKey, "MQTT") ?? defaultTopic;
        _logger = logger;
        _historyInsertService = historyInsertService;
    }

    public string MachineName { get; }

    public string Topic { get; }

    public bool CanHandle(string topic) =>
        string.Equals(topic, Topic, StringComparison.OrdinalIgnoreCase);

    public async Task<long> InsertAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var record = LeakTestPayloadMapper.ToHistoryRecord(topic, payload);
        var insertedId = await _historyInsertService.InsertAsync(record, cancellationToken);

        _logger.LogInformation(
            "[{Machine}] Inserted history Id={Id} EngineNo={EngineNumber} PressureInput={PressureInput} Topic={Topic}",
            MachineName,
            insertedId,
            record.EngineNumber,
            record.PressureInput,
            topic);

        return insertedId;
    }
}
