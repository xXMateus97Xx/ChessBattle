namespace Chess.Uci.Connector;

public class Settings
{
    public static Settings Default => field ??= new();
    public string EnginePath { get; set; }
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public Dictionary<string, string> Options { get; set; } = [];

    private static string[] ReadFileLines(string path)
    {
        return File.ReadAllLines(path);
    }

    public static Dictionary<string, string> LoadOptions(string path)
    {
        var options = new Dictionary<string, string>();

        foreach (var line in ReadFileLines(path).Where(x => !x.StartsWith('#') && !string.IsNullOrWhiteSpace(x)))
        {
            var lineParsed = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (lineParsed.Length != 2)
                throw new ApplicationException($"Invalid engine option line: {line}");

            options[lineParsed[0]] = lineParsed[1];
        }

        return options;
    }
}
