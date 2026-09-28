using System.Text;

namespace Chess.Battle;

public enum GameResult : byte
{
    Tie,
    White,
    Black
}

public readonly record struct MoveResult(bool IsCapture, bool IsShortRock, bool IsLongRock, bool Check, string Disambiguation);

public class Chessboard
{
    public const char WhiteKing = 'K';
    public const char BlackKing = 'k';
    public const char WhiteQueen = 'Q';
    public const char BlackQueen = 'q';
    public const char WhitePawn = 'P';
    public const char BlackPawn = 'p';
    public const char WhiteBishop = 'B';
    public const char BlackBishop = 'b';
    public const char WhiteKnight = 'N';
    public const char BlackKnight = 'n';
    public const char WhiteHook = 'R';
    public const char BlackHook = 'r';

    private readonly char[,] _board;
    private readonly List<char> _whitePieces;
    private readonly List<char> _blackPieces;
    private bool _whiteRound;
    private int _halfmoveClock;

    private const int WhiteShortRight = 1;
    private const int WhiteLongRight = 2;
    private const int BlackShortRight = 4;
    private const int BlackLongRight = 8;

    private int _castlingRights;
    private int _enPassantFile = -1;
    private bool _isThreefoldRepetition;
    private readonly Dictionary<string, int> _positionCounts = new(StringComparer.Ordinal);

    public Chessboard()
    {
        _board = new char[8, 8];
        _whiteRound = true;
        _castlingRights = WhiteShortRight | WhiteLongRight | BlackShortRight | BlackLongRight;

        for (int i = 0; i < 8; i++)
        {
            _board[1, i] = BlackPawn;
            _board[6, i] = WhitePawn;
        }

        _board[7, 0] = WhiteHook;
        _board[7, 1] = WhiteKnight;
        _board[7, 2] = WhiteBishop;
        _board[7, 3] = WhiteQueen;
        _board[7, 4] = WhiteKing;
        _board[7, 5] = WhiteBishop;
        _board[7, 6] = WhiteKnight;
        _board[7, 7] = WhiteHook;

        _board[0, 0] = BlackHook;
        _board[0, 1] = BlackKnight;
        _board[0, 2] = BlackBishop;
        _board[0, 3] = BlackQueen;
        _board[0, 4] = BlackKing;
        _board[0, 5] = BlackBishop;
        _board[0, 6] = BlackKnight;
        _board[0, 7] = BlackHook;

        _whitePieces = new List<char>(16)
        {
            WhiteHook, WhiteHook,
            WhiteBishop, WhiteBishop,
            WhiteKnight, WhiteKnight,
            WhiteKing, WhiteQueen,
            WhitePawn, WhitePawn, WhitePawn, WhitePawn, WhitePawn, WhitePawn, WhitePawn, WhitePawn
        };
        _blackPieces = new List<char>(16)
        {
            BlackHook, BlackHook,
            BlackBishop, BlackBishop,
            BlackKnight, BlackKnight,
            BlackKing, BlackQueen,
            BlackPawn, BlackPawn, BlackPawn, BlackPawn, BlackPawn, BlackPawn, BlackPawn, BlackPawn
        };

        RecordPosition(false);
    }

    public Chessboard(string fen)
    {
        _board = new char[8, 8];
        _whitePieces = new List<char>(16);
        _blackPieces = new List<char>(16);

        var currentIndex = 0;
        for (var i = 0; i < 8; i++)
        {
            var index = fen.IndexOf('/', currentIndex);
            if (index == -1)
                index = fen.IndexOf(' ', currentIndex);
            var line = fen.AsSpan().Slice(currentIndex, index - currentIndex);
            currentIndex = index + 1;

            for (int j = 0, k = 0; k < line.Length; j++, k++)
            {
                var c = line[k];
                if (char.IsDigit(c))
                {
                    j += c - '0' - 1;
                    continue;
                }

                _board[i, j] = c;
                if (char.IsUpper(c))
                    _whitePieces.Add(c);
                else
                    _blackPieces.Add(c);
            }
        }

        _whiteRound = fen[currentIndex] == 'w';

        var fields = fen.Split(' ');
        if (fields.Length > 2)
        {
            var castling = fields[2];
            if (castling.Contains(WhiteKing))
                _castlingRights |= WhiteShortRight;
            if (castling.Contains(WhiteQueen))
                _castlingRights |= WhiteLongRight;
            if (castling.Contains(BlackKing))
                _castlingRights |= BlackShortRight;
            if (castling.Contains(BlackQueen))
                _castlingRights |= BlackLongRight;
        }

        RecordPosition(false);
    }

    public MoveResult DoMove(string move)
    {
        var isCapture = false;
        var isShortRock = false;
        var isLongRock = false;
        var disambiguation = "";
        bool check;

        if (move == "(none)")
        {
            check = _whiteRound ? IsBlackInCheck() : IsWhiteInCheck();
            return new MoveResult(isCapture, isShortRock, isLongRock, check, disambiguation);
        }

        var fromCol = move[0] - 'a';
        var fromLine = 8 - (move[1] - '0');
        var toCol = move[2] - 'a';
        var toLine = 8 - (move[3] - '0');

        if (_board[toLine, toCol] != '\0')
        {
            (_whiteRound ? _blackPieces : _whitePieces).Remove(_board[toLine, toCol]);
            isCapture = true;
        }

        var isPawnMove = move.Length == 5;

        if (move.Length == 5)
        {
            var promotion = move[4];
            if (_whiteRound)
                promotion = (char)(promotion - 32);

            var pieces = _whiteRound ? _whitePieces : _blackPieces;
            pieces.Remove(_whiteRound ? WhitePawn : BlackPawn);
            pieces.Add(promotion);
            _board[toLine, toCol] = promotion;
        }
        else
        {
            var currentPiece = _board[fromLine, fromCol];
            isShortRock = IsShortRock(move, currentPiece);
            isLongRock = IsLongRock(move, currentPiece);
            disambiguation = GetDisambiguation(currentPiece, fromLine, fromCol, toLine, toCol);
            isPawnMove = currentPiece == WhitePawn || currentPiece == BlackPawn;

            if (isPawnMove && fromCol != toCol && !isCapture)
            {
                (_whiteRound ? _blackPieces : _whitePieces).Remove(_board[fromLine, toCol]);
                _board[fromLine, toCol] = '\0';
                isCapture = true;
            }

            _board[toLine, toCol] = _board[fromLine, fromCol];

            if (isShortRock)
            {
                if (_whiteRound)
                {
                    _board[7, 7] = '\0';
                    _board[7, 5] = WhiteHook;
                }
                else
                {
                    _board[0, 7] = '\0';
                    _board[0, 5] = BlackHook;
                }
            }
            else if (isLongRock)
            {
                if (_whiteRound)
                {
                    _board[7, 0] = '\0';
                    _board[7, 3] = WhiteHook;
                }
                else
                {
                    _board[0, 0] = '\0';
                    _board[0, 3] = BlackHook;
                }
            }
        }

        _board[fromLine, fromCol] = '\0';

        UpdateCastlingRights(fromLine, fromCol);
        UpdateCastlingRights(toLine, toCol);

        _enPassantFile = isPawnMove && Math.Abs(toLine - fromLine) == 2 && HasAdjacentEnemyPawn(toLine, toCol) ? toCol : -1;

        var irreversible = isCapture || isPawnMove;
        _halfmoveClock = irreversible ? 0 : _halfmoveClock + 1;

        check = _whiteRound ? IsBlackInCheck() : IsWhiteInCheck();

        _whiteRound = !_whiteRound;

        RecordPosition(irreversible);

        return new MoveResult(isCapture, isShortRock, isLongRock, check, disambiguation);
    }

    private void UpdateCastlingRights(int line, int col)
    {
        _castlingRights &= (line, col) switch
        {
            (7, 4) => ~(WhiteShortRight | WhiteLongRight),
            (0, 4) => ~(BlackShortRight | BlackLongRight),
            (7, 7) => ~WhiteShortRight,
            (7, 0) => ~WhiteLongRight,
            (0, 7) => ~BlackShortRight,
            (0, 0) => ~BlackLongRight,
            _ => ~0
        };
    }

    private bool HasAdjacentEnemyPawn(int line, int col)
    {
        var enemyPawn = _whiteRound ? BlackPawn : WhitePawn;

        return (col > 0 && _board[line, col - 1] == enemyPawn) ||
            (col < 7 && _board[line, col + 1] == enemyPawn);
    }

    public bool IsThreefoldRepetition() => _isThreefoldRepetition;

    private void RecordPosition(bool irreversible)
    {
        if (irreversible)
            _positionCounts.Clear();

        var key = GetPositionKey();
        var count = _positionCounts.GetValueOrDefault(key) + 1;
        _positionCounts[key] = count;
        _isThreefoldRepetition = count >= 3;
    }

    private string GetPositionKey()
    {
        return string.Create(67, this, static (span, board) =>
        {
            for (var i = 0; i < 64; i++)
            {
                var piece = board._board[i / 8, i % 8];
                span[i] = piece == '\0' ? '.' : piece;
            }

            span[64] = board._whiteRound ? 'w' : 'b';
            span[65] = (char)('0' + board._castlingRights);
            span[66] = board._enPassantFile < 0 ? '-' : (char)('a' + board._enPassantFile);
        });
    }

    private static readonly (int, int)[] KnightOffsets = [(2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2)];
    private static readonly (int, int)[] DiagonalDirections = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
    private static readonly (int, int)[] StraightDirections = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int, int)[] AllDirections = [.. DiagonalDirections, .. StraightDirections];

    private string GetDisambiguation(char piece, int fromLine, int fromCol, int toLine, int toCol)
    {
        var type = char.ToUpperInvariant(piece);
        if (type is not (WhiteQueen or WhiteHook or WhiteBishop or WhiteKnight))
            return "";

        var pieces = _whiteRound ? _whitePieces : _blackPieces;
        var count = 0;
        for (var i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == piece)
                count++;
        }

        if (count < 2)
            return "";

        var directions = type switch
        {
            WhiteKnight => KnightOffsets,
            WhiteBishop => DiagonalDirections,
            WhiteHook => StraightDirections,
            _ => AllDirections
        };

        var ambiguous = false;
        var sameFile = false;
        var sameLine = false;

        foreach (var (lineStep, colStep) in directions)
        {
            var line = toLine + lineStep;
            var col = toCol + colStep;

            while (line >= 0 && line < 8 && col >= 0 && col < 8)
            {
                var current = _board[line, col];
                if (current != '\0')
                {
                    if (current == piece && (line != fromLine || col != fromCol) &&
                        IsLegalAlternative(piece, line, col, toLine, toCol))
                    {
                        ambiguous = true;
                        sameFile |= col == fromCol;
                        sameLine |= line == fromLine;
                    }

                    break;
                }

                if (type == WhiteKnight)
                    break;

                line += lineStep;
                col += colStep;
            }
        }

        if (!ambiguous)
            return "";

        var file = (char)('a' + fromCol);
        var rank = (char)('0' + 8 - fromLine);

        if (!sameFile)
            return file.ToString();
        if (!sameLine)
            return rank.ToString();
        return string.Concat(file, rank);
    }

    private bool IsLegalAlternative(char piece, int line, int col, int toLine, int toCol)
    {
        var captured = _board[toLine, toCol];
        _board[toLine, toCol] = piece;
        _board[line, col] = '\0';

        var inCheck = _whiteRound ? IsWhiteInCheck() : IsBlackInCheck();

        _board[line, col] = piece;
        _board[toLine, toCol] = captured;

        return !inCheck;
    }

    public GameResult GetWinner()
    {
        if (IsTechnicalTie())
            return GameResult.Tie;

        if (IsBlackInCheck())
            return GameResult.White;

        if (IsWhiteInCheck())
            return GameResult.Black;

        return GameResult.Tie;
    }

    public char FindPieceAtPosition(string pos)
    {
        var col = pos[0] - 'a';
        var line = 8 - (pos[1] - '0');
        return _board[line, col];
    }

    public bool IsBlackInCheck()
    {
        return IsInCheck(BlackKing);
    }

    public bool IsWhiteInCheck()
    {
        return IsInCheck(WhiteKing);
    }

    public bool IsFiftyMoveRule() => _halfmoveClock >= 100;

    public bool IsTechnicalTie()
    {
        if (_blackPieces.Count == 1 && _whitePieces.Count == 1)
            return true;

        return !HasMinimumPiecesToWin(_whitePieces, WhiteQueen, WhiteKnight, WhitePawn, WhiteHook, WhiteBishop) &&
            !HasMinimumPiecesToWin(_blackPieces, BlackQueen, BlackKnight, BlackPawn, BlackHook, BlackBishop);
    }

    private static bool HasMinimumPiecesToWin(List<char> pieces, char queen, char horse, char pawn, char hook, char bishop)
    {
        var bishops = 0;
        var horses = 0;
        for (var i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece == queen || piece == hook || piece == pawn)
                return true;

            if (piece == horse)
                horses++;
            else if (piece == bishop)
                bishops++;
        }

        if (bishops >= 2)
            return true;

        if (horses >= 1 && bishops >= 1)
            return true;

        return false;
    }

    private bool IsInCheck(char king)
    {
        var position = FindPosition(king);

        char bishop, horse, hook, queen;
        if (king == BlackKing)
        {
            bishop = WhiteBishop;
            horse = WhiteKnight;
            hook = WhiteHook;
            queen = WhiteQueen;
        }
        else
        {
            bishop = BlackBishop;
            horse = BlackKnight;
            hook = BlackHook;
            queen = BlackQueen;
        }

        var piecesInLine = FindNearestPiecesInLine(position.Item1, position.Item2);
        if (VerifyCheckInLineOrColumn(piecesInLine, queen, hook))
            return true;

        var piecesInColumn = FindNearestPiecesInColumn(position.Item2, position.Item1);
        if (VerifyCheckInLineOrColumn(piecesInColumn, queen, hook))
            return true;

        return IsHorseAround(position, horse) || IsBishopAround(position, bishop) || IsBishopAround(position, queen) || IsPawnAround(king, position);
    }

    private static bool VerifyCheckInLineOrColumn(char[] nearestPieces, char queen, char hook)
    {
        for (var j = 0; j < nearestPieces.Length; j++)
        {
            if (nearestPieces[j] == '\0')
                continue;

            if (nearestPieces[j] == queen || nearestPieces[j] == hook)
                return true;
        }

        return false;
    }

    private char[] FindNearestPiecesInLine(int line, int piecePos)
    {
        var result = new char[2];
        var pos = 0;
        for (int i = 0; i < 8; i++)
        {
            if (i == piecePos)
            {
                pos++;
                continue;
            }

            var currentPiece = _board[line, i];
            if (currentPiece == '\0')
                continue;

            result[pos] = currentPiece;
            if (pos == 1)
                break;
        }

        return result;
    }

    private bool IsBishopAround((int, int) pos, char bishop)
    {
        var currentPosition = (pos.Item1 - 1, pos.Item2 - 1);
        while (currentPosition.Item1 >= 0 && currentPosition.Item2 >= 0)
        {
            var piece = _board[currentPosition.Item1, currentPosition.Item2];
            if (piece == bishop)
                return true;

            if (piece != '\0')
                break;

            currentPosition.Item1--;
            currentPosition.Item2--;
        }

        currentPosition = (pos.Item1 + 1, pos.Item2 + 1);
        while (currentPosition.Item1 < 8 && currentPosition.Item2 < 8)
        {
            var piece = _board[currentPosition.Item1, currentPosition.Item2];
            if (piece == bishop)
                return true;

            if (piece != '\0')
                break;

            currentPosition.Item1++;
            currentPosition.Item2++;
        }

        currentPosition = (pos.Item1 + 1, pos.Item2 - 1);
        while (currentPosition.Item1 < 8 && currentPosition.Item2 >= 0)
        {
            var piece = _board[currentPosition.Item1, currentPosition.Item2];
            if (piece == bishop)
                return true;

            if (piece != '\0')
                break;

            currentPosition.Item1++;
            currentPosition.Item2--;
        }

        currentPosition = (pos.Item1 - 1, pos.Item2 + 1);
        while (currentPosition.Item1 >= 0 && currentPosition.Item2 < 8)
        {
            var piece = _board[currentPosition.Item1, currentPosition.Item2];
            if (piece == bishop)
                return true;

            if (piece != '\0')
                break;

            currentPosition.Item1--;
            currentPosition.Item2++;
        }

        return false;
    }

    private bool IsPawnAround(char king, (int, int) position)
    {
        if (king == BlackKing)
            return position.Item1 < 7 && ((position.Item2 > 0 && _board[position.Item1 + 1, position.Item2 - 1] == WhitePawn) ||
                (position.Item2 < 7 && _board[position.Item1 + 1, position.Item2 + 1] == WhitePawn));

        return position.Item1 > 0 && ((position.Item2 > 0 && _board[position.Item1 - 1, position.Item2 - 1] == BlackPawn) ||
            (position.Item2 < 7 && _board[position.Item1 - 1, position.Item2 + 1] == BlackPawn));
    }

    private bool IsHorseAround((int, int) pos, char horse)
    {
        Span<(int, int)> possiblePositions =
        [
            (pos.Item1 + 2, pos.Item2 + 1),
            (pos.Item1 + 2, pos.Item2 - 1),
            (pos.Item1 - 2, pos.Item2 + 1),
            (pos.Item1 - 2, pos.Item2 - 1),
            (pos.Item1 + 1, pos.Item2 + 2),
            (pos.Item1 + 1, pos.Item2 - 2),
            (pos.Item1 - 1, pos.Item2 + 2),
            (pos.Item1 - 1, pos.Item2 - 2),
        ];

        for (var i = 0; i < possiblePositions.Length; i++)
        {
            var possiblePosition = possiblePositions[i];
            if (possiblePosition.Item1 < 0 || possiblePosition.Item1 >= 8 ||
                possiblePosition.Item2 < 0 || possiblePosition.Item2 >= 8)
                continue;

            if (_board[possiblePosition.Item1, possiblePosition.Item2] == horse)
                return true;
        }

        return false;
    }

    private char[] FindNearestPiecesInColumn(int column, int pieceLine)
    {
        var result = new char[2];
        var pos = 0;
        for (var i = 0; i < 8; i++)
        {
            if (i == pieceLine)
            {
                pos++;
                continue;
            }

            var currentPiece = _board[i, column];
            if (currentPiece == '\0')
                continue;

            result[pos] = currentPiece;
            if (pos == 1)
                break;
        }

        return result;
    }

    private static bool IsShortRock(string move, char piece) => (piece == WhiteKing && move == "e1g1") || piece == BlackKing && move == "e8g8";
    private static bool IsLongRock(string move, char piece) => (piece == WhiteKing && move == "e1c1") || piece == BlackKing && move == "e8c8";

    private (int, int) FindPosition(char piece)
    {
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                if (_board[i, j] == piece)
                    return (i, j);
            }
        }

        return (-1, -1);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < 8; i++)
        {
            sb.AppendLine("+---+---+---+---+---+---+---+---+");

            for (int j = 0; j < 8; j++)
            {
                var pos = _board[i, j];
                if (pos == '\0')
                    pos = ' ';
                sb.Append("| ");
                sb.Append(pos);
                sb.Append(' ');
            }
            sb.AppendLine("|");
        }

        sb.AppendLine("+---+---+---+---+---+---+---+---+");

        return sb.ToString();
    }
}