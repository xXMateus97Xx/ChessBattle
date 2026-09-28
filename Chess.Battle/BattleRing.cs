using Chess.Uci.Connector;
using System.Diagnostics;
using System.Text;

namespace Chess.Battle;

public class BattleRing
{
    private static readonly TimeSpan MoveTimeSafetyMargin = TimeSpan.FromSeconds(5);

    private readonly BattleSettings _settings;
    private readonly Chessboard _board;
    private readonly ILogger _logger;
    private readonly INotationWriter _notationWriter;

    public BattleRing(BattleSettings settings,
        ILogger logger,
        INotationWriter notationWriter)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _board = new Chessboard();
        notationWriter.Board = _board;
        _logger = logger;
        _notationWriter = notationWriter;
    }

    public void Battle()
    {
        TouchOutput();
        InitializeEngines(out UCIBattleConfiguration white, out UCIBattleConfiguration black);

        try
        {
            _notationWriter.WriteHeader(white, black);
            DoBattle(white, black);
            SaveResult();
        }
        finally
        {
            UnloadEngines(white, black);
        }
    }

    private void TouchOutput()
    {
        using (File.Create(_settings.OutputPath)) { }
    }

    private void InitializeEngines(out UCIBattleConfiguration white, out UCIBattleConfiguration black)
    {
        var readTimeout = GetEffectiveReadTimeout();
        _settings.Engine1.ReadTimeout = readTimeout;
        _settings.Engine2.ReadTimeout = readTimeout;

        var uci1 = new UCIBattleConfiguration
        {
            UCI = new UCIConnector(_settings.Engine1),
            Name = _settings.Engine1.Name
        };

        var uci2 = new UCIBattleConfiguration
        {
            UCI = new UCIConnector(_settings.Engine2),
            Name = _settings.Engine2.Name
        };

        LogSettings();

        try
        {
            var connect1 = Measure(uci1.UCI.Connect);
            var connect2 = Measure(uci2.UCI.Connect);

            var newGame1 = Measure(uci1.UCI.StartGame);
            var newGame2 = Measure(uci2.UCI.StartGame);

            LogConnection(1, uci1, connect1, newGame1);
            LogConnection(2, uci2, connect2, newGame2);
        }
        catch
        {
            uci1.UCI.Dispose();
            uci2.UCI.Dispose();
            throw;
        }

        white = _settings.WhiteEngine == 1 ? uci1 : uci2;
        black = _settings.WhiteEngine == 1 ? uci2 : uci1;

        _logger.LogInfo("White: {0}, Black: {1}", white.Name, black.Name);
    }

    private TimeSpan GetEffectiveReadTimeout()
    {
        if (_settings.Timeout == Timeout.InfiniteTimeSpan)
            return Timeout.InfiniteTimeSpan;

        var minimumTimeout = _settings.MoveTime + MoveTimeSafetyMargin;
        return _settings.Timeout < minimumTimeout ? minimumTimeout : _settings.Timeout;
    }

    private void LogSettings()
    {
        _logger.LogInfo("Output: {0}", _settings.OutputPath);
        _logger.LogInfo("Move time (both engines): {0}", _settings.MoveTime);
        _logger.LogInfo("Read timeout (both engines): {0}", FormatTimeout(GetEffectiveReadTimeout()));

        LogEngineSettings(1, _settings.Engine1);
        LogEngineSettings(2, _settings.Engine2);
    }

    private void LogEngineSettings(int number, EngineSettings engine)
    {
        _logger.LogInfo("Engine {0} settings: name={1}, path={2}, threads={3}, read timeout={4}",
            number, engine.Name, engine.EnginePath, engine.Threads, FormatTimeout(engine.ReadTimeout));
    }

    private void LogConnection(int number, UCIBattleConfiguration engine, TimeSpan connect, TimeSpan newGame)
    {
        _logger.LogInfo("Engine {0} UCI: connected={1}, handshake in {2}, new game ready in {3}",
            number, engine.UCI.IsConnected, connect, newGame);
    }

    private static string FormatTimeout(TimeSpan timeout) =>
        timeout == Timeout.InfiniteTimeSpan ? "none" : timeout.ToString();

    private static TimeSpan Measure(Action action)
    {
        var start = Stopwatch.GetTimestamp();
        action();
        return Stopwatch.GetElapsedTime(start);
    }

    private static void UnloadEngines(UCIBattleConfiguration white, UCIBattleConfiguration black)
    {
        white.UCI.Dispose();
        black.UCI.Dispose();
    }

    private void SaveResult()
    {
        var result = _notationWriter.GetResult();
        File.WriteAllText(_settings.OutputPath, result, Encoding.UTF8);
    }

    private void DoBattle(UCIBattleConfiguration white, UCIBattleConfiguration black)
    {
        var history = "";
        string move;
        var endGame = false;
        var whiteturn = true;
        var next = white;

        while (!endGame)
        {
            move = GetMove(next, history);
            var moveResult = _board.DoMove(move);
            endGame = AnalyseMove(move, moveResult);
            _notationWriter.WriteMove(move, moveResult);
            next = whiteturn ? black : white;
            whiteturn = !whiteturn;
            history += move + " ";
        }

        _logger.LogInfo(_board.ToString());
    }

    private string GetMove(UCIBattleConfiguration uci, string history)
    {
        var start = Stopwatch.GetTimestamp();

        var move = uci.UCI.GetNextMove(history, (int)_settings.MoveTime.TotalMilliseconds);

        var elapsed = Stopwatch.GetElapsedTime(start);

        _logger.LogInfo("Move {0} generated in: {1}", move, elapsed);

        return move;
    }

    private bool AnalyseMove(string move, MoveResult moveResult)
    {
        string reason;

        if (move == "(none)")
        {
            reason = _board.GetWinner() switch
            {
                GameResult.White => "Checkmate, White wins",
                GameResult.Black => "Checkmate, Black wins",
                _ => "Stalemate"
            };
        }
        else if (_board.IsTechnicalTie())
            reason = "Draw by insufficient material";
        else if (moveResult.Check)
            return false;
        else if (_board.IsFiftyMoveRule())
            reason = "Draw by fifty-move rule";
        else if (_board.IsThreefoldRepetition())
            reason = "Draw by threefold repetition";
        else
            return false;

        _logger.LogInfo("Game over: {0}", reason);

        return true;
    }
}
