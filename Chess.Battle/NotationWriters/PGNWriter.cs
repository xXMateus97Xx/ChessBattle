using System.Text;

namespace Chess.Battle.NotationWriters;

public class PGNWriter : INotationWriter
{
    private readonly StringBuilder _header;
    private readonly StringBuilder _rounds;
    private bool _whiteRound, _resultWrote;
    private int _currentRound;
    private int _checkSymbolIndex = -1;
    private string _result = "*";

    public PGNWriter()
    {
        _header = new StringBuilder();
        _rounds = new StringBuilder();
        _whiteRound = true;
        _currentRound = 1;
    }

    public Chessboard Board
    {
        get => field ?? throw new InvalidOperationException();
        set
        {
            if (field is not null)
                throw new InvalidOperationException();

            field = value;
        }
    }

    public void WriteMove(string move, MoveResult moveResult)
    {
        var board = Board;

        if (move == "(none)")
        {
            if (_checkSymbolIndex >= 0)
            {
                _rounds[_checkSymbolIndex] = '#';
                _checkSymbolIndex = -1;
            }

            return;
        }

        var destinyPos = move.Substring(2, 2);
        var piece = board.FindPieceAtPosition(destinyPos);

        var capture = moveResult.IsCapture ? "x" : "";
        var check = moveResult.Check ? "+" : "";

        if (_whiteRound)
            _rounds.AppendFormat("{0}. ", _currentRound);

        if (moveResult.IsLongRock)
            _rounds.AppendFormat("O-O-O{0} ", check);
        else if (moveResult.IsShortRock)
            _rounds.AppendFormat("O-O{0} ", check);
        else if (move.Length == 5)
            _rounds.AppendFormat("{0}{1}{2}={3}{4} ", moveResult.IsCapture ? move[0] : "", capture, destinyPos, char.ToUpperInvariant(move[4]), check);
        else if (piece == Chessboard.WhitePawn || piece == Chessboard.BlackPawn)
            _rounds.AppendFormat("{0}{1}{2}{3} ", moveResult.IsCapture ? move[0] : "", capture, destinyPos, check);
        else
            _rounds.AppendFormat("{0}{1}{2}{3}{4} ", char.ToUpperInvariant(piece), moveResult.Disambiguation, capture, destinyPos, check);

        _checkSymbolIndex = moveResult.Check ? _rounds.Length - 2 : -1;

        if (!_whiteRound)
            _currentRound++;

        _whiteRound = !_whiteRound;
    }

    public void WriteHeader(UCIBattleConfiguration white, UCIBattleConfiguration black)
    {
        if (_header.Length > 0)
            return;

        AppendTag("Event", "UCI Battle");
        AppendTag("Site", Environment.MachineName);
        AppendTag("Date", DateTime.Now.ToString("yyyy.MM.dd"));
        AppendTag("Round", "1");
        AppendTag("White", white.Name);
        AppendTag("Black", black.Name);
    }

    private void WriteResultToHeader()
    {
        if (_resultWrote)
            return;

        _resultWrote = true;

        _result = Board.GetWinner() switch
        {
            GameResult.White => "1-0",
            GameResult.Black => "0-1",
            _ => "1/2-1/2"
        };

        AppendTag("Result", _result);
        _header.AppendLine();
    }

    public string GetResult()
    {
        WriteResultToHeader();

        var sb = new StringBuilder();
        sb.Append(_header);
        sb.Append(_rounds);
        sb.Append(_result);

        return sb.ToString();
    }

    private void AppendTag(string name, string value)
    {
        var escaped = (value ?? "?").Replace("\\", "\\\\").Replace("\"", "\\\"");
        _header.Append('[').Append(name).Append(" \"").Append(escaped).AppendLine("\"]");
    }
}