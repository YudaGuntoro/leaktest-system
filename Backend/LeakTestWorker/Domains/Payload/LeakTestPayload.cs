using Newtonsoft.Json.Linq;

namespace LeakTestWorker.Domains.Payload;

public sealed class LeakTestPayload
{
    public required string Topic { get; init; }

    public required string RawJson { get; init; }

    public required JObject Data { get; init; }
}
