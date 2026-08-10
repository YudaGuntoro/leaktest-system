using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace LeakTestWorker.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileLogWriter _writer;

    public FileLoggerProvider(string logDirectory)
    {
        _writer = new FileLogWriter(logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _writer));

    public void Dispose()
    {
        _loggers.Clear();
        _writer.Dispose();
    }
}

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLogWriter _writer;

    public FileLogger(string categoryName, FileLogWriter writer)
    {
        _categoryName = categoryName;
        _writer = writer;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        _writer.Write(logLevel, _categoryName, eventId, message, exception);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public sealed class FileLogWriter : IDisposable
{
    private readonly object _sync = new();
    private readonly string _logDirectory;

    public FileLogWriter(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void Write(
        LogLevel logLevel,
        string categoryName,
        EventId eventId,
        string message,
        Exception? exception)
    {
        var now = DateTimeOffset.Now;
        var filePath = Path.Combine(_logDirectory, $"worker-{now:yyyyMMdd}.log");
        var line = BuildLine(now, logLevel, categoryName, eventId, message, exception);

        lock (_sync)
        {
            Directory.CreateDirectory(_logDirectory);
            File.AppendAllText(filePath, line, Encoding.UTF8);
        }
    }

    public void Dispose()
    {
    }

    private static string BuildLine(
        DateTimeOffset timestamp,
        LogLevel logLevel,
        string categoryName,
        EventId eventId,
        string message,
        Exception? exception)
    {
        var builder = new StringBuilder();
        builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        builder.Append(" [");
        builder.Append(logLevel);
        builder.Append("] ");
        builder.Append(categoryName);

        if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
        {
            builder.Append(" EventId=");
            builder.Append(eventId.Id);

            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                builder.Append('/');
                builder.Append(eventId.Name);
            }
        }

        builder.Append(" - ");
        builder.AppendLine(message);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        return builder.ToString();
    }
}
