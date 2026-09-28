using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Chess.Uci.Connector;

public partial class UCIConnector : IDisposable
{
    private static readonly TimeSpan QuitTimeout = TimeSpan.FromSeconds(2);

    private const string MoveCommandPattern = "position startpos moves {0}\ngo movetime {1}\n";
    private const string MoveByFenCommandPattern = "position fen {0}\ngo movetime {1}\n";
    private const string UciCommand = "uci\n";
    private const string IsReadyCommand = "isready\n";
    private const string QuitCommand = "quit\n";
    private const string NewGameCommand = "ucinewgame\n";
    private const string SetThreadsCommand = "setoption name Threads value {0}\n";

    [GeneratedRegex("bestmove (?<move>([a-h][1-8]){2}[rnbq]?|[(]none[)])")]
    private static partial Regex BestMoveRegex();

    [GeneratedRegex(@"^\s*uciok\s*$")]
    private static partial Regex UciOkRegex();

    [GeneratedRegex(@"^\s*readyok\s*$")]
    private static partial Regex ReadyOkRegex();

    private readonly Settings _settings;
    private Process _process;
    private bool _isConnected;
    private readonly Lock _lock = new();

    public UCIConnector()
    {
        _settings = Settings.Default;
    }

    public UCIConnector(Settings settings)
    {
        _settings = settings;
    }

    public bool IsConnected => _isConnected;

    public void Connect()
    {
        if (_isConnected)
            return;

        Process process = null;

        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = _settings.EnginePath
                }
            };

            process.ErrorDataReceived += static (_, _) => { };

            if (!process.Start())
                throw new InvalidOperationException("Failed to start process");

            process.BeginErrorReadLine();

            _process = process;
            _isConnected = true;

            WriteCommand(UciCommand);
            ReadResponse(UciOkRegex());

            ConfigureEngine();
            WaitUntilReady();
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _process = null;
            Terminate(process, graceful: false);

            throw new UCIConnectionException("Could not connect to UCI engine, verify inner exception for more details", ex);
        }
    }

    public void StartGame()
    {
        WriteCommand(NewGameCommand);
        WaitUntilReady();
    }

    private void WaitUntilReady()
    {
        WriteCommand(IsReadyCommand);
        ReadResponse(ReadyOkRegex());
    }

    public string GetNextMove(string moves, int movetimeMs)
    {
        var cmd = string.Format(MoveCommandPattern, moves, movetimeMs);
        return GetUciMove(cmd);
    }

    public string GetNextMoveByFen(string fen, int movetimeMs)
    {
        var cmd = string.Format(MoveByFenCommandPattern, fen, movetimeMs);
        return GetUciMove(cmd);
    }

    private string GetUciMove(string cmd)
    {
        WriteCommand(cmd);
        return ReadResponse(BestMoveRegex(), "move");
    }

    public void Disconnect()
    {
        if (!_isConnected)
            return;

        var process = _process;
        _isConnected = false;
        _process = null;

        try
        {
            if (!process.HasExited)
                process.StandardInput.Write(QuitCommand);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {

        }

        Terminate(process, graceful: true);
    }

    public void Dispose()
    {
        Disconnect();
    }

    private static void Terminate(Process process, bool graceful)
    {
        if (process is null)
            return;

        try
        {
            if (!graceful || !process.WaitForExit(QuitTimeout))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(QuitTimeout);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {

        }
        finally
        {
            process.Dispose();
        }
    }

    public void ConfigureEngine()
    {
        var cmd = string.Format(SetThreadsCommand, _settings.Threads);
        WriteCommand(cmd);
    }

    private void WriteCommand(string cmd)
    {
        VerifyConnection();

        lock (_lock)
            _process.StandardInput.Write(cmd);
    }

    private string ReadResponse(Regex lineRegex, string resultGroup = null)
    {
        VerifyConnection();

        lock (_lock)
        {
            var output = _process.StandardOutput;
            var start = Stopwatch.GetTimestamp();

            while (true)
            {
                var line = ReadLine(output, start);

                var match = lineRegex.Match(line);

                if (!match.Success)
                    continue;

                return resultGroup is null ? line : match.Groups[resultGroup].Value;
            }
        }
    }

    private string ReadLine(StreamReader output, long start)
    {
        var remaining = _settings.ReadTimeout == Timeout.InfiniteTimeSpan
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromTicks(Math.Max((_settings.ReadTimeout - Stopwatch.GetElapsedTime(start)).Ticks, 0));

        string line;

        try
        {
            line = output.ReadLineAsync().WaitAsync(remaining).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            Disconnect();
            throw new UCIConnectionException($"UCI engine did not answer within {_settings.ReadTimeout}");
        }

        if (line is null)
        {
            Disconnect();
            throw new UCIConnectionException("UCI connection was closed");
        }

        return line;
    }

    private void VerifyConnection()
    {
        var process = _process;

        if (!_isConnected || process is null)
            throw new UCIConnectionException("UCI is not yet connected");

        if (process.HasExited)
            throw new UCIConnectionException("UCI connection was closed");
    }
}
