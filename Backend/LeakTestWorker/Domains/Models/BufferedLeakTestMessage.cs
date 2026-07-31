namespace LeakTestWorker.Domains.Models;

public sealed class BufferedLeakTestMessage
{
    public DateTime BufferedAt { get; init; } = DateTime.Now;

    public string Topic { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public string Error { get; init; } = string.Empty;
}
