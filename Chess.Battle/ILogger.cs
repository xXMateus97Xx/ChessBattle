namespace Chess.Battle;

public interface ILogger : IDisposable
{
    void LogInfo(string format, params ReadOnlySpan<object> args);
}
