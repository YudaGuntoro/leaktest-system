using LeakTestWorker.MQTT.Handlers.Service;
using LeakTestWorker.MQTT.Handlers.SQL;

namespace LeakTestWorker.MQTT.Handlers;

public sealed class LeakTestHandler : LeakTestMachineHandlerBase
{
    public LeakTestHandler(
        ILogger<LeakTestHandler> logger,
        ILeakTestHistoryInsertService historyInsertService)
        : base("LeakTest", "Topic", "/LeakTest", logger, historyInsertService)
    {
    }
}
