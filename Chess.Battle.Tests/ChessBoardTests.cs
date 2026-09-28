using NUnit.Framework;
using System.Text;

namespace Chess.Battle.Tests;

public class ChessBoardTests
{
    #region Helpers

    private static string BuildFen(IEnumerable<string> placements)
    {
        var grid = new char[8, 8];
        foreach (var placement in placements)
            grid[8 - (placement[2] - '0'), placement[1] - 'a'] = placement[0];

        var sb = new StringBuilder();
        for (var row = 0; row < 8; row++)
        {
            var empty = 0;
            for (var col = 0; col < 8; col++)
            {
                if (grid[row, col] == '\0')
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }

                sb.Append(grid[row, col]);
            }

            if (empty > 0)
                sb.Append(empty);
            if (row < 7)
                sb.Append('/');
        }

        return sb.Append(" w - - 0 1").ToString();
    }

    private static string SwapCase(string text) =>
        string.Concat(text.Select(c => char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c)));

    private static string Mirror(string fen)
    {
        var fields = fen.Split(' ');
        var ranks = fields[0].Split('/');
        Array.Reverse(ranks);

        fields[0] = string.Join('/', ranks.Select(SwapCase));
        fields[1] = fields[1] == "w" ? "b" : "w";
        fields[2] = SwapCase(fields[2]);

        return string.Join(' ', fields);
    }

    private static string MirrorMove(string move) =>
        string.Concat(move.Select(c => char.IsDigit(c) ? (char)('0' + 9 - (c - '0')) : c));

    private static void AssertCheck(bool expected, IEnumerable<string> placements)
    {
        var fen = BuildFen(placements);
        Assert.That(new Chessboard(fen).IsBlackInCheck(), Is.EqualTo(expected), "black king, " + fen);

        var mirrored = Mirror(fen);
        Assert.That(new Chessboard(mirrored).IsWhiteInCheck(), Is.EqualTo(expected), "white king, " + mirrored);
    }

    private static MoveResult Play(Chessboard board, string moves)
    {
        MoveResult last = default;
        foreach (var move in moves.Split(' '))
            last = board.DoMove(move);

        return last;
    }

    private static void Shuffle(Chessboard board, int plies, string[] cycle)
    {
        for (var i = 0; i < plies; i++)
            board.DoMove(cycle[i % cycle.Length]);
    }

    private static readonly string[] RookCycle = ["a1a2", "e8d8", "a2a1", "d8e8"];

    #endregion

    #region Check: existing scenarios

    [Test]
    public void BlackInCheckByBishop()
    {
        var board = new Chessboard("rnbqkbnr/ppp2ppp/8/1B1pp3/3PP3/8/PPP2PPP/RNBQK1NR w KQkq - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void BlackInCheckByKnight()
    {
        var board = new Chessboard("3qk3/8/5N2/4K3/8/2Q5/8/8 w - - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void BlackInCheckByHook()
    {
        var board = new Chessboard("3qk2R/8/8/5K2/6Q1/8/8/8 w - - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void BlackInCheckByPawn()
    {
        var board = new Chessboard("3b3R/8/8/3k1K2/4P1Q1/8/8/8 w - - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void BlackInCheckByPawnColumnA()
    {
        var board = new Chessboard("3b3R/8/8/k4K2/1P6/2Q5/8/8 w - - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void BlackInCheckByPawnColumnG()
    {
        var board = new Chessboard("3b4/8/8/3R1K1k/6P1/2Q5/8/8 w - - 0 1");

        Assert.True(board.IsBlackInCheck());
    }

    [Test]
    public void WhiteInCheckByBishop()
    {
        var board = new Chessboard("2bqk3/7R/8/5K2/6Q1/8/8/8 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    [Test]
    public void WhiteInCheckByKnight()
    {
        var board = new Chessboard("3qk3/7R/3n4/8/4K1Q1/8/8/8 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    [Test]
    public void WhiteInCheckByHook()
    {
        var board = new Chessboard("3qk3/7R/8/2K5/6Q1/8/8/2r5 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    [Test]
    public void WhiteInCheckByPawn()
    {
        var board = new Chessboard("4k3/1q6/1p6/2K5/6Q1/8/5R2/8 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    [Test]
    public void WhiteInCheckByPawnColumnA()
    {
        var board = new Chessboard("4k3/8/8/1pq5/K5Q1/8/5R2/8 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    [Test]
    public void WhiteInCheckByPawnColumnG()
    {
        var board = new Chessboard("4k3/7q/6p1/2Q4K/8/8/5R2/8 w - - 0 1");

        Assert.True(board.IsWhiteInCheck());
    }

    #endregion

    #region Check: every direction, both colors (black king on d4, mirrored for white)

    [Test]
    public void BishopAndQueenCheckOnAllDiagonals(
        [Values("a1", "g7", "a7", "g1", "e5", "c3", "c5", "e3")] string square,
        [Values('B', 'Q')] char piece)
    {
        AssertCheck(true, ["kd4", "Kh4", $"{piece}{square}"]);
    }

    [Test]
    public void RookAndQueenCheckOnFilesAndRanks(
        [Values("d8", "d1", "a4", "g4", "d5", "d3", "c4", "e4")] string square,
        [Values('R', 'Q')] char piece)
    {
        AssertCheck(true, ["kd4", "Kh4", $"{piece}{square}"]);
    }

    [Test]
    public void KnightChecksFromAllEightSquares(
        [Values("b3", "b5", "c2", "c6", "e2", "e6", "f3", "f5")] string square)
    {
        AssertCheck(true, ["kd4", "Kh4", $"N{square}"]);
    }

    [TestCase("kd4", "Pc3")]
    [TestCase("kd4", "Pe3")]
    [TestCase("ka4", "Pb3")]
    [TestCase("kh4", "Pg3")]
    [TestCase("kd8", "Pc7")]
    [TestCase("kd8", "Pe7")]
    [TestCase("ka8", "Pb7")]
    [TestCase("kh8", "Pg7")]
    public void PawnChecks(string king, string pawn)
    {
        AssertCheck(true, [king, pawn, "Kh6"]);
    }

    [TestCase("kd4", "Pc5")]
    [TestCase("kd4", "Pe5")]
    [TestCase("kd4", "Pd3")]
    [TestCase("kd4", "Pc4")]
    [TestCase("kd4", "Pe4")]
    [TestCase("kd1", "Pc2")]
    [TestCase("kd1", "Pe2")]
    [TestCase("ka1", "Pb2")]
    [TestCase("kh1", "Pg2")]
    public void PawnDoesNotCheck(string king, string pawn)
    {
        AssertCheck(false, [king, pawn, "Kh6"]);
    }

    [TestCase("Rd8 Bd6 Rd1")]
    [TestCase("Rd1 Bd2 Rd8")]
    [TestCase("Ra4 Nb4 Rg4")]
    [TestCase("Ba1 Pb2 Bg7")]
    public void CheckByOneOfTwoSlidersWhenTheOtherIsBlocked(string pieces)
    {
        AssertCheck(true, [.. pieces.Split(' '), "kd4", "Kh4"]);
    }

    #endregion

    #region No check

    [TestCase("Ba1 pb2")]
    [TestCase("Ba1 Pb2")]
    [TestCase("Bg7 Ne5")]
    [TestCase("Qa7 pb6")]
    [TestCase("Qg1 pe3")]
    [TestCase("Rd8 pd6")]
    [TestCase("Rd8 Pd6")]
    [TestCase("Rd1 Nd2")]
    [TestCase("Ra4 Bb4")]
    [TestCase("Rg4 pf4")]
    [TestCase("Qd8 Pd7")]
    public void BlockedSlidingPieceDoesNotCheck(string pieces)
    {
        AssertCheck(false, [.. pieces.Split(' '), "kd4", "Kh4"]);
    }

    [TestCase("Bb1")]
    [TestCase("Bc2")]
    [TestCase("Bd6")]
    [TestCase("Ba4")]
    [TestCase("Re1")]
    [TestCase("Ra5")]
    [TestCase("Rc3")]
    [TestCase("Rc5")]
    [TestCase("Re5")]
    [TestCase("Qb1")]
    [TestCase("Qe2")]
    [TestCase("Nb4")]
    [TestCase("Nd6")]
    [TestCase("Nc3")]
    [TestCase("Nd2")]
    [TestCase("Ne4")]
    [TestCase("Nb2")]
    public void PieceThatDoesNotAttackTheKing(string piece)
    {
        AssertCheck(false, [piece, "kd4", "Kh4"]);
    }

    [TestCase("bg7")]
    [TestCase("ra4")]
    [TestCase("rd8")]
    [TestCase("qa1")]
    [TestCase("nb3")]
    [TestCase("pc3")]
    public void OwnPiecesDoNotCheck(string piece)
    {
        AssertCheck(false, [piece, "kd4", "Kh4"]);
    }

    [Test]
    public void InitialPositionHasNoCheck()
    {
        var board = new Chessboard();

        Assert.That(board.IsWhiteInCheck(), Is.False);
        Assert.That(board.IsBlackInCheck(), Is.False);
    }

    #endregion

    #region FindPieceAtPosition

    [TestCase("a1", 'R')]
    [TestCase("e1", 'K')]
    [TestCase("d1", 'Q')]
    [TestCase("c1", 'B')]
    [TestCase("g1", 'N')]
    [TestCase("h2", 'P')]
    [TestCase("a8", 'r')]
    [TestCase("e8", 'k')]
    [TestCase("d8", 'q')]
    [TestCase("f8", 'b')]
    [TestCase("b8", 'n')]
    [TestCase("c7", 'p')]
    [TestCase("e4", '\0')]
    [TestCase("h5", '\0')]
    public void FindPieceAtPositionInInitialBoard(string square, char expected)
    {
        Assert.That(new Chessboard().FindPieceAtPosition(square), Is.EqualTo(expected));
    }

    #endregion

    #region DoMove: captures, en passant, promotion

    [Test]
    public void QuietMoveIsNotCaptureNorCheck()
    {
        var result = new Chessboard().DoMove("e2e4");

        Assert.That(result, Is.EqualTo(new MoveResult(false, false, false, false, "")));
    }

    [Test]
    public void MoveUpdatesTheBoard()
    {
        var board = new Chessboard();
        board.DoMove("e2e4");

        Assert.That(board.FindPieceAtPosition("e2"), Is.EqualTo('\0'));
        Assert.That(board.FindPieceAtPosition("e4"), Is.EqualTo('P'));
    }

    [Test]
    public void PawnCaptureIsCapture()
    {
        var board = new Chessboard();
        var result = Play(board, "e2e4 d7d5 e4d5");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(board.FindPieceAtPosition("d5"), Is.EqualTo('P'));
    }

    [Test]
    public void BlackCaptureIsCapture()
    {
        var result = Play(new Chessboard(), "e2e4 d7d5 a2a3 d5e4");

        Assert.That(result.IsCapture, Is.True);
    }

    [Test]
    public void CapturedPieceLeavesThePieceList()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/3r4/4K3 w - - 0 1");
        Assert.That(board.IsTechnicalTie(), Is.False);

        var result = board.DoMove("e1d2");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(board.IsTechnicalTie(), Is.True);
    }

    [Test]
    public void WhiteEnPassant()
    {
        var board = new Chessboard("4k3/8/8/3pP3/8/8/8/4K3 w - - 0 1");

        var result = board.DoMove("e5d6");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(board.FindPieceAtPosition("d6"), Is.EqualTo('P'));
        Assert.That(board.FindPieceAtPosition("d5"), Is.EqualTo('\0'));
        Assert.That(board.FindPieceAtPosition("e5"), Is.EqualTo('\0'));
    }

    [Test]
    public void BlackEnPassant()
    {
        var board = new Chessboard("4k3/8/8/8/3p4/8/4P3/4K3 w - - 0 1");

        board.DoMove("e2e4");
        var result = board.DoMove("d4e3");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(board.FindPieceAtPosition("e3"), Is.EqualTo('p'));
        Assert.That(board.FindPieceAtPosition("e4"), Is.EqualTo('\0'));
        Assert.That(board.FindPieceAtPosition("d4"), Is.EqualTo('\0'));
    }

    [Test]
    public void EnPassantCanGiveCheck()
    {
        var board = new Chessboard("8/4k3/8/3pP3/8/8/8/4K3 w - - 0 1");

        var result = board.DoMove("e5d6");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(result.Check, Is.True);
    }

    [TestCase("7k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7a8q", 'a', 8, 'Q', true, false)]
    [TestCase("7k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7a8r", 'a', 8, 'R', true, false)]
    [TestCase("7k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7a8b", 'a', 8, 'B', false, false)]
    [TestCase("7k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7a8n", 'a', 8, 'N', false, false)]
    [TestCase("1r5k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7b8q", 'b', 8, 'Q', true, true)]
    [TestCase("1r5k/P7/8/8/8/8/8/4K3 w - - 0 1", "a7b8n", 'b', 8, 'N', false, true)]
    [TestCase("4k3/8/8/8/8/8/p7/4K3 b - - 0 1", "a2a1q", 'a', 1, 'q', true, false)]
    [TestCase("4k3/8/8/8/8/8/p7/4K3 b - - 0 1", "a2a1n", 'a', 1, 'n', false, false)]
    [TestCase("4k3/8/8/8/8/8/p7/1R2K3 b - - 0 1", "a2b1q", 'b', 1, 'q', true, true)]
    public void Promotion(string fen, string move, char file, int rank, char expectedPiece, bool expectedCheck, bool expectedCapture)
    {
        var board = new Chessboard(fen);

        var result = board.DoMove(move);

        Assert.That(board.FindPieceAtPosition($"{file}{rank}"), Is.EqualTo(expectedPiece));
        Assert.That(board.FindPieceAtPosition(move[..2]), Is.EqualTo('\0'));
        Assert.That(result.Check, Is.EqualTo(expectedCheck));
        Assert.That(result.IsCapture, Is.EqualTo(expectedCapture));
    }

    #endregion

    #region DoMove: castling

    [TestCase("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1g1", true, "g1", "f1", "h1")]
    [TestCase("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1", "e1c1", false, "c1", "d1", "a1")]
    public void Castling(string fen, string move, bool shortSide, string kingSquare, string rookSquare, string oldRookSquare)
    {
        foreach (var (position, castleMove) in new[] { (fen, move), (Mirror(fen), MirrorMove(move)) })
        {
            var board = new Chessboard(position);
            var mirrored = castleMove != move;
            var king = mirrored ? 'k' : 'K';
            var rook = mirrored ? 'r' : 'R';

            var result = board.DoMove(castleMove);

            Assert.That(result.IsShortRock, Is.EqualTo(shortSide));
            Assert.That(result.IsLongRock, Is.EqualTo(!shortSide));
            Assert.That(board.FindPieceAtPosition(mirrored ? MirrorMove(kingSquare) : kingSquare), Is.EqualTo(king));
            Assert.That(board.FindPieceAtPosition(mirrored ? MirrorMove(rookSquare) : rookSquare), Is.EqualTo(rook));
            Assert.That(board.FindPieceAtPosition(mirrored ? MirrorMove(oldRookSquare) : oldRookSquare), Is.EqualTo('\0'));
            Assert.That(board.FindPieceAtPosition(mirrored ? "e8" : "e1"), Is.EqualTo('\0'));
        }
    }

    [Test]
    public void KingStepIsNotCastling()
    {
        var result = new Chessboard("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1").DoMove("e1f1");

        Assert.That(result.IsShortRock, Is.False);
        Assert.That(result.IsLongRock, Is.False);
    }

    [Test]
    public void CastlingCanGiveCheck()
    {
        var result = new Chessboard("5k2/8/8/8/8/8/8/4K2R w K - 0 1").DoMove("e1g1");

        Assert.That(result.IsShortRock, Is.True);
        Assert.That(result.Check, Is.True);
    }

    #endregion

    #region DoMove: disambiguation

    [TestCase("4k3/8/8/8/8/8/8/1N2KN2 w - - 0 1", "b1d2", "b")]
    [TestCase("4k3/8/8/8/8/8/8/1N2KN2 w - - 0 1", "f1d2", "f")]
    [TestCase("4k3/8/8/1N6/8/8/8/1N2K3 w - - 0 1", "b1c3", "1")]
    [TestCase("4k3/8/8/1N6/8/8/8/1N2K3 w - - 0 1", "b5c3", "5")]
    [TestCase("4k3/8/8/8/8/8/8/1N1NK3 w - - 0 1", "b1c3", "b")]
    [TestCase("4k3/8/8/8/8/8/8/1N1NK3 w - - 0 1", "d1c3", "d")]
    [TestCase("4k3/8/8/8/8/8/1B3B2/4K3 w - - 0 1", "b2d4", "b")]
    [TestCase("4k3/8/8/8/8/8/1B3B2/4K3 w - - 0 1", "f2d4", "f")]
    [TestCase("4k3/8/8/8/1B6/8/1B6/4K3 w - - 0 1", "b2a3", "2")]
    [TestCase("4k3/8/8/8/1B6/8/1B6/4K3 w - - 0 1", "b4a3", "4")]
    [TestCase("4k3/8/8/8/8/8/4K3/R6R w - - 0 1", "a1d1", "a")]
    [TestCase("4k3/8/8/8/8/8/4K3/R6R w - - 0 1", "h1d1", "h")]
    [TestCase("4k3/R7/8/8/8/8/8/R3K3 w - - 0 1", "a1a4", "1")]
    [TestCase("4k3/R7/8/8/8/8/8/R3K3 w - - 0 1", "a7a4", "7")]
    [TestCase("4k3/8/8/8/8/Q7/8/Q1Q4K w - - 0 1", "a1c3", "a1")]
    [TestCase("4k3/8/8/8/8/Q7/8/Q1Q4K w - - 0 1", "a3c3", "3")]
    [TestCase("4k3/8/8/8/8/Q7/8/Q1Q4K w - - 0 1", "c1c3", "c")]
    public void Disambiguation(string fen, string move, string expected)
    {
        Assert.That(new Chessboard(fen).DoMove(move).Disambiguation, Is.EqualTo(expected));
        Assert.That(new Chessboard(Mirror(fen)).DoMove(MirrorMove(move)).Disambiguation, Is.EqualTo(MirrorMove(expected)));
    }

    [TestCase("4k3/8/8/8/8/8/4K3/R3B2R w - - 0 1", "a1d1")]       // the other rook is blocked
    [TestCase("4k3/8/8/8/8/8/8/R3K2R w - - 0 1", "a1a5")]          // the other rook cannot reach the square
    [TestCase("4k3/8/8/8/8/8/8/1N2K3 w - - 0 1", "b1c3")]          // only one knight
    [TestCase("4r2k/8/8/8/4N3/8/8/1N2K3 w - - 0 1", "b1d2")]       // the other knight is pinned
    [TestCase("4k3/8/8/8/8/8/8/1N2K3 w - - 0 1", "e1e2")]          // the king is never disambiguated
    [TestCase("4k3/8/8/8/8/8/P6P/4K3 w - - 0 1", "a2a4")]          // neither are pawns
    public void NoDisambiguation(string fen, string move)
    {
        Assert.That(new Chessboard(fen).DoMove(move).Disambiguation, Is.Empty);
        Assert.That(new Chessboard(Mirror(fen)).DoMove(MirrorMove(move)).Disambiguation, Is.Empty);
    }

    [Test]
    public void InitialPositionHasNoDisambiguation()
    {
        var board = new Chessboard();

        Assert.That(board.DoMove("g1f3").Disambiguation, Is.Empty);
        Assert.That(board.DoMove("g8f6").Disambiguation, Is.Empty);
        Assert.That(board.DoMove("b1c3").Disambiguation, Is.Empty);
    }

    #endregion

    #region DoMove: check, checkmate, stalemate

    [Test]
    public void MoveThatChecks()
    {
        var result = new Chessboard("4k3/8/8/8/8/8/4R3/4K3 w - - 0 1").DoMove("e2e7");

        Assert.That(result.Check, Is.True);
    }

    [Test]
    public void MoveThatDoesNotCheck()
    {
        var result = new Chessboard("4k3/8/8/8/8/8/4R3/4K3 w - - 0 1").DoMove("e2a2");

        Assert.That(result.Check, Is.False);
    }

    [Test]
    public void DiscoveredCheck()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/4B3/4R1K1 w - - 0 1");
        Assert.That(board.IsBlackInCheck(), Is.False);

        var result = board.DoMove("e2d3");

        Assert.That(result.Check, Is.True);
    }

    [Test]
    public void BackRankMate()
    {
        var board = new Chessboard("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");

        var result = board.DoMove("a1a8");

        Assert.That(result.Check, Is.True);
        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.White));
    }

    [Test]
    public void FoolsMate()
    {
        var board = new Chessboard();

        var result = Play(board, "f2f3 e7e5 g2g4 d8h4");

        Assert.That(result.Check, Is.True);
        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.Black));
    }

    [Test]
    public void ScholarsMate()
    {
        var board = new Chessboard();

        var result = Play(board, "e2e4 e7e5 d1h5 b8c6 f1c4 g8f6 h5f7");

        Assert.That(result.Check, Is.True);
        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.White));
    }

    [Test]
    public void NoneMoveDoesNotChangeTheBoard()
    {
        var board = new Chessboard("6k1/5ppp/8/8/8/8/8/R3K3 w - - 0 1");
        board.DoMove("a1a8");

        Assert.DoesNotThrow(() => board.DoMove("(none)"));
        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.White));
    }

    [Test]
    public void Stalemate()
    {
        var board = new Chessboard("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");

        Assert.That(board.IsBlackInCheck(), Is.False);
        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.Tie));
    }

    [Test]
    public void InitialPositionIsATie()
    {
        Assert.That(new Chessboard().GetWinner(), Is.EqualTo(GameResult.Tie));
    }

    [Test]
    public void CheckWithoutMateStillReportsTheChecker()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/4R3/4K3 w - - 0 1");
        board.DoMove("e2e7");

        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.White));
    }

    #endregion

    #region Insufficient material

    [TestCase("4k3/8/8/8/8/8/8/4K3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/2B1K3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/1N2K3 w - - 0 1")]
    [TestCase("2b1k3/8/8/8/8/8/8/4K3 w - - 0 1")]
    [TestCase("1n2k3/8/8/8/8/8/8/4K3 w - - 0 1")]
    public void InsufficientMaterial(string fen)
    {
        Assert.That(new Chessboard(fen).IsTechnicalTie(), Is.True);
    }

    [TestCase("4k3/8/8/8/8/8/8/R3K3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/3QK3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/2B1KB2 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/1NB1K3 w - - 0 1")]
    [TestCase("r3k3/8/8/8/8/8/8/4K3 w - - 0 1")]
    [TestCase("4k3/p7/8/8/8/8/8/1N2K3 w - - 0 1")]
    [TestCase("4k3/8/8/8/8/8/8/2B1K1R1 w - - 0 1")]
    public void SufficientMaterial(string fen)
    {
        Assert.That(new Chessboard(fen).IsTechnicalTie(), Is.False);
    }

    [Test]
    public void InsufficientMaterialResultsInATieEvenWhenInCheck()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/8/2B1K3 w - - 0 1");

        Assert.That(board.GetWinner(), Is.EqualTo(GameResult.Tie));
    }

    [Test]
    public void PromotedPieceLeavesThePawnOutOfTheList()
    {
        // The promoted pawn must stop counting as a pawn once it turns into a queen.
        var board = new Chessboard("7k/P7/8/8/8/8/8/4K3 w - - 0 1");

        board.DoMove("a7a8q");

        Assert.That(board.IsTechnicalTie(), Is.False);
    }

    [Test]
    public void CapturingAPromotedPieceCanResultInATie()
    {
        // Once the promoted queen is captured, only kings remain, so material becomes insufficient.
        var board = new Chessboard("1k6/P7/8/8/8/8/8/4K3 w - - 0 1");

        board.DoMove("a7a8q");

        Assert.That(board.IsTechnicalTie(), Is.False);

        board.DoMove("b8a8");

        Assert.That(board.IsTechnicalTie(), Is.True);
    }

    #endregion

    #region Fifty-move rule

    private const string RookEndgame = "4k3/8/8/8/8/8/8/R3K3 w - - 0 1";

    [Test]
    public void FiftyMoveRuleIsNotReachedInitially()
    {
        Assert.That(new Chessboard().IsFiftyMoveRule(), Is.False);
    }

    [Test]
    public void FiftyMoveRuleIsNotReachedAt99Plies()
    {
        var board = new Chessboard(RookEndgame);

        Shuffle(board, 99, RookCycle);

        Assert.That(board.IsFiftyMoveRule(), Is.False);
    }

    [Test]
    public void FiftyMoveRuleIsReachedAt100Plies()
    {
        var board = new Chessboard(RookEndgame);

        Shuffle(board, 100, RookCycle);

        Assert.That(board.IsFiftyMoveRule(), Is.True);
    }

    [Test]
    public void PawnMoveResetsTheFiftyMoveClock()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/7P/R3K3 w - - 0 1");

        Shuffle(board, 98, RookCycle);
        board.DoMove("h2h3");
        board.DoMove("d8e8");
        board.DoMove("a2a1");
        board.DoMove("e8d8");

        Assert.That(board.IsFiftyMoveRule(), Is.False);
    }

    [Test]
    public void CaptureResetsTheFiftyMoveClock()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/n7/R3K3 w - - 0 1");

        Shuffle(board, 98, ["a1a3", "e8d8", "a3a1", "d8e8"]);
        var result = board.DoMove("a3a2");

        Assert.That(result.IsCapture, Is.True);
        Assert.That(board.IsFiftyMoveRule(), Is.False);
    }

    #endregion

    #region Threefold repetition

    [Test]
    public void NoRepetitionInitially()
    {
        Assert.That(new Chessboard().IsThreefoldRepetition(), Is.False);
    }

    [Test]
    public void ThirdOccurrenceIsARepetition()
    {
        var board = new Chessboard(RookEndgame);

        Shuffle(board, 4, RookCycle);
        Assert.That(board.IsThreefoldRepetition(), Is.False);

        Shuffle(board, 3, RookCycle);
        Assert.That(board.IsThreefoldRepetition(), Is.False);

        board.DoMove("d8e8");
        Assert.That(board.IsThreefoldRepetition(), Is.True);
    }

    [Test]
    public void RepetitionRequiresTheSameSideToMove()
    {
        // The same placement with a different side to move is a different position.
        var board = new Chessboard(RookEndgame);

        Shuffle(board, 2, ["a1a2", "e8d8"]);
        Shuffle(board, 2, ["a2a1", "d8e8"]);
        Shuffle(board, 2, ["a1a2", "e8d8"]);

        Assert.That(board.IsThreefoldRepetition(), Is.False);
    }

    [Test]
    public void LosingCastlingRightsBreaksTheRepetition()
    {
        // After the first cycle both rooks are back, but the castling rights are gone.
        var board = new Chessboard("r3k3/8/8/8/8/8/8/R3K3 w Qq - 0 1");
        var cycle = new[] { "a1a2", "a8a7", "a2a1", "a7a8" };

        Shuffle(board, 8, cycle);
        Assert.That(board.IsThreefoldRepetition(), Is.False);

        Shuffle(board, 4, cycle);
        Assert.That(board.IsThreefoldRepetition(), Is.True);
    }

    [Test]
    public void PawnMoveResetsRepetitionHistory()
    {
        var board = new Chessboard("4k3/8/8/8/8/8/7P/R3K3 w - - 0 1");

        Shuffle(board, 4, RookCycle);
        board.DoMove("h2h3");
        board.DoMove("e8d8");
        board.DoMove("a1a2");
        board.DoMove("d8e8");
        board.DoMove("a2a1");
        board.DoMove("e8d8");
        board.DoMove("a1a2");
        board.DoMove("d8e8");

        Assert.That(board.IsThreefoldRepetition(), Is.False);
    }

    #endregion
}
