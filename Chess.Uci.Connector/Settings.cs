using System.Globalization;
using System.Text;

namespace Chess.Uci.Connector;

public class Settings
{
    private const string CONFIG_FILE = "config.conf";

    private static Settings _settings;
    public static Settings Default => _settings ??= Load(CONFIG_FILE);

    private string _path;

    public int Threads { get; set; }
    public string EnginePath { get; set; }
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public void Save()
    {
        ParseAndValidateEnginePath(EnginePath);
        ParseAndValidateThreads(Threads.ToString());
        ParseAndValidateReadTimeout(ReadTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture));

        var lines = ReadFileLines(_path);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("threads", StringComparison.Ordinal))
                lines[i] = $"threads={Threads}";

            if (line.StartsWith("engine", StringComparison.Ordinal))
                lines[i] = $"engine={EnginePath}";

            if (line.StartsWith("readtimeout", StringComparison.Ordinal))
                lines[i] = $"readtimeout={ReadTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)}";
        }

        File.WriteAllLines(_path, lines, Encoding.UTF8);
    }

    public static Settings Load(string configPath)
    {
        return ParseConfig(configPath);
    }

    private static string[] ReadFileLines(string path)
    {
        return File.ReadAllLines(path);
    }

    private static Settings ParseConfig(string path)
    {
        var settings = new Settings();
        var lines = ReadFileLines(path);

        foreach (var line in lines.Where(x => !x.StartsWith("#", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(x)))
        {
            var lineParsed = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (lineParsed.Length < 2)
                ThrowInvalidConfigException(lineParsed.Length == 0 ? line : lineParsed[0], "null");

            switch (lineParsed[0])
            {
                case "threads":
                    settings.Threads = ParseAndValidateThreads(lineParsed[1]);
                    break;
                case "engine":
                    settings.EnginePath = ParseAndValidateEnginePath(lineParsed[1]);
                    break;
                case "readtimeout":
                    settings.ReadTimeout = ParseAndValidateReadTimeout(lineParsed[1]);
                    break;
                default:
                    throw new ApplicationException($"Key is not valid {lineParsed[0]}");
            }
        }

        settings._path = path;
        return settings;
    }

    private static string ParseAndValidateEnginePath(string value)
    {
        if (!File.Exists(value))
            ThrowInvalidConfigException("engine", value);

        return value;
    }

    private static int ParseAndValidateThreads(string value)
    {
        if (!int.TryParse(value, out var threads) || threads <= 0)
            ThrowInvalidConfigException("threads", value);

        return threads;
    }

    private static TimeSpan ParseAndValidateReadTimeout(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
            ThrowInvalidConfigException("readtimeout", value);

        return seconds == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(seconds);
    }

    private static void ThrowInvalidConfigException(string key, string value)
    {
        throw new ApplicationException($"Value {value} is not valid for key {key}");
    }
}
