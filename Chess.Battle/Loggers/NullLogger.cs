namespace Chess.Battle.Loggers;

public class NullLogger : ILogger
{
    public void LogInfo(string format, params ReadOnlySpan<object> args) { }

    public void Dispose() { }
}
