using LeakTestWorker.Domains.Models;
using LeakTestWorker.Helper;
using LeakTestWorker.MQTT.Handlers.SQL;
using LeakTestWorker.MQTT.Interfaces;
using LeakTestWorker.Singletone;
using Newtonsoft.Json;

namespace LeakTestWorker.MQTT.Handlers.Service;

public sealed class LeakTestMessageRouter : ILeakTestMessageHandler
{
    private readonly ILogger<LeakTestMessageRouter> _logger;
    private readonly ILeakTestHistoryInsertService _historyInsertService;
    private readonly ILeakTestMessageBuffer _buffer;
    private readonly IReadOnlyList<ILeakTestMachineHandler> _machineHandlers;
    private readonly int _maxReprocessBatch;

    public LeakTestMessageRouter(
        ILogger<LeakTestMessageRouter> logger,
        ILeakTestHistoryInsertService historyInsertService,
        ILeakTestMessageBuffer buffer,
        IEnumerable<ILeakTestMachineHandler> machineHandlers)
    {
        _logger = logger;
        _historyInsertService = historyInsertService;
        _buffer = buffer;
        _machineHandlers = machineHandlers.ToList();
        _maxReprocessBatch = Math.Max(1, Config.Instance.ReadInt("MaxReprocessBatch", "Buffer", 100));
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default) =>
        _historyInsertService.WaitUntilReadyAsync(cancellationToken);

    public async Task HandleAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var handler = ResolveHandler(topic);

        try
        {
            await handler.InsertAsync(topic, payload, cancellationToken);
            await ReprocessBufferAsync(cancellationToken);
        }
        catch (Exception ex) when (DbRetry.IsDatabaseException(ex) && !cancellationToken.IsCancellationRequested)
        {
            await BufferAsync(topic, payload, ex, cancellationToken);
        }
    }

    public async Task ReprocessBufferAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var processed = await _buffer.ProcessAsync(ProcessBufferedMessageAsync, _maxReprocessBatch, cancellationToken);
            if (processed > 0)
            {
                _logger.LogInformation("[Buffer] Reprocessed {Count} buffered message(s).", processed);
            }
        }
        catch (Exception ex) when (DbRetry.IsDatabaseException(ex) && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "[Buffer] Database is still not ready. Buffered messages will be retried.");
        }
    }

    private async Task<bool> ProcessBufferedMessageAsync(
        BufferedLeakTestMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = ResolveHandler(message.Topic);
            await handler.InsertAsync(message.Topic, message.Payload, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            _logger.LogError(ex, "[Buffer] Invalid buffered payload dropped. Topic={Topic}", message.Topic);
            return true;
        }
        catch (Exception ex) when (!DbRetry.IsDatabaseException(ex) && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[Buffer] Buffered payload dropped because it is not recoverable. Topic={Topic}", message.Topic);
            return true;
        }
    }

    private async Task BufferAsync(
        string topic,
        string payload,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await _buffer.EnqueueAsync(new BufferedLeakTestMessage
        {
            BufferedAt = DateTime.Now,
            Topic = topic,
            Payload = payload,
            Error = exception.Message
        }, cancellationToken);

        _logger.LogWarning(
            exception,
            "[Buffer] Database insert failed. Payload buffered for retry. Topic={Topic}",
            topic);
    }

    private ILeakTestMachineHandler ResolveHandler(string topic)
    {
        var handler = _machineHandlers.FirstOrDefault(item => item.CanHandle(topic));
        if (handler is not null)
        {
            return handler;
        }

        handler = _machineHandlers.FirstOrDefault();
        if (handler is null)
        {
            throw new InvalidOperationException("No MQTT machine handler is registered.");
        }

        _logger.LogWarning(
            "[MQTT] Topic {Topic} has no specific machine handler. Using {Machine}.",
            topic,
            handler.MachineName);

        return handler;
    }
}
