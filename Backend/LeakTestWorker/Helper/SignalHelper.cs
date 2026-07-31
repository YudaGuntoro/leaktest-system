namespace LeakTestWorker.Helper;

public static class SignalHelper
{
    public static string TopicToMachineName(string topic)
    {
        var value = string.IsNullOrWhiteSpace(topic)
            ? "unknown"
            : topic.Trim().Trim('/').Replace('/', '-');

        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }
}
