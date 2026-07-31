using System.Runtime.ExceptionServices;
using LeakTestWorker.Domains.Models;
using LeakTestWorker.MQTT.Interfaces;
using LeakTestWorker.Singletone;
using Newtonsoft.Json;

namespace LeakTestWorker.MQTT.Handlers.Service;

public sealed class FileLeakTestMessageBuffer : ILeakTestMessageBuffer
{
    private const string DefaultBufferPath = "buffer/leaktest-history-buffer.jsonl";

    private readonly ILogger<FileLeakTestMessageBuffer> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;

    public FileLeakTestMessageBuffer(ILogger<FileLeakTestMessageBuffer> logger)
    {
        _logger = logger;
        _filePath = ResolvePath(Config.Instance.Read("FilePath", "Buffer"));
    }

    public async Task EnqueueAsync(BufferedLeakTestMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            EnsureDirectory();
            var line = JsonConvert.SerializeObject(message);
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<int> ProcessAsync(
        Func<BufferedLeakTestMessage, CancellationToken, Task<bool>> processor,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var messages = await ReadAllUnlockedAsync(cancellationToken);
            if (messages.Count == 0)
            {
                return 0;
            }

            var remaining = new List<BufferedLeakTestMessage>();
            var processed = 0;
            Exception? failure = null;

            for (var index = 0; index < messages.Count; index++)
            {
                if (processed >= maxItems)
                {
                    remaining.AddRange(messages.Skip(index));
                    break;
                }

                try
                {
                    var removeFromBuffer = await processor(messages[index], cancellationToken);
                    if (removeFromBuffer)
                    {
                        processed++;
                        continue;
                    }

                    remaining.AddRange(messages.Skip(index));
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failure = ex;
                    remaining.AddRange(messages.Skip(index));
                    break;
                }
            }

            await WriteAllUnlockedAsync(remaining, cancellationToken);

            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            return processed;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<BufferedLeakTestMessage>> ReadAllUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(_filePath, cancellationToken);
        var messages = new List<BufferedLeakTestMessage>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var message = JsonConvert.DeserializeObject<BufferedLeakTestMessage>(line);
                if (message is not null && !string.IsNullOrWhiteSpace(message.Payload))
                {
                    messages.Add(message);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[Buffer] Corrupt buffer line skipped.");
            }
        }

        return messages;
    }

    private async Task WriteAllUnlockedAsync(
        IReadOnlyCollection<BufferedLeakTestMessage> messages,
        CancellationToken cancellationToken)
    {
        EnsureDirectory();

        if (messages.Count == 0)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            return;
        }

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        var lines = messages.Select(JsonConvert.SerializeObject);
        await File.WriteAllLinesAsync(tempPath, lines, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private void EnsureDirectory()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string ResolvePath(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultBufferPath
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);
    }
}
