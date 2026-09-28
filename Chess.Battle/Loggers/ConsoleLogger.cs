namespace Chess.Battle.Loggers;

public class ConsoleLogger : ILogger
{
    public void LogInfo(string format, params ReadOnlySpan<object> args) => Console.WriteLine(format, args);

    public void Dispose() { }
}
