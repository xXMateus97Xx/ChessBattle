using System.Text;

namespace Chess.Battle.Loggers;

public sealed class FileLogger : ILogger
{
    private readonly StreamWriter _writer;

    public FileLogger(string directory = "")
    {
        FilePath = Path.Combine(directory, $"Battle-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt");

        var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public string FilePath { get; }

    public void LogInfo(string format, params ReadOnlySpan<object> args) => _writer.WriteLine(format, args);

    public void Dispose() => _writer.Dispose();
}
