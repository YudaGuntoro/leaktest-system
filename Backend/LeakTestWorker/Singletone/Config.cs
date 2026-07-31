namespace LeakTestWorker.Singletone;

public sealed class Config
{
    private readonly Lazy<IReadOnlyDictionary<string, Dictionary<string, string>>> _sections =
        new(LoadSections, LazyThreadSafetyMode.ExecutionAndPublication);

    public static Config Instance { get; } = new();

    private Config()
    {
    }

    public string? Read(string key, string section)
    {
        if (!_sections.Value.TryGetValue(section, out var values))
        {
            return null;
        }

        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    public int ReadInt(string key, string section, int defaultValue)
    {
        var value = Read(key, section);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    public IReadOnlyList<string> ReadList(string key, string section)
    {
        var value = Read(key, section);
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static IReadOnlyDictionary<string, Dictionary<string, string>> LoadSections()
    {
        var filePath = ResolveSettingsPath();
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var currentSection = string.Empty;

        if (!File.Exists(filePath))
        {
            return sections;
        }

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                sections.TryAdd(currentSection, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || string.IsNullOrWhiteSpace(currentSection))
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            sections[currentSection][key] = value;
        }

        return sections;
    }

    private static string ResolveSettingsPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Settings.ini"),
            Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini")
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
