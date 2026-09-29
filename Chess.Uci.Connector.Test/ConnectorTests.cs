using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Chess.Uci.Connector.Test;

public class ConnectorTests
{
    private const string MOVE_REGEX_PATTERN = "([a-h][1-8]){2}[rnbq]?";

    private Settings _setting;

    [SetUp]
    public void Setup()
    {
        _setting = new Settings
        {
            EnginePath = "stockfish",
            Options = new Dictionary<string, string> { ["Threads"] = "2" }
        };
    }

    [Test]
    public void ShouldConnectToUciEngine()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();

        Assert.IsTrue(connector.IsConnected);
    }

    [Test]
    public void ShouldConnectAndDisconnectToUciEngine()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();
        connector.Disconnect();

        Assert.IsFalse(connector.IsConnected);
    }

    [Test]
    public void ShouldGetNextMoveAsWhite()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();

        var move = connector.GetNextMove("e2e4", 500);

        connector.Disconnect();

        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move);
    }

    [Test]
    public void ShouldGetNextMoveAsWhiteWithShortMoveTime()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();

        var move = connector.GetNextMove("e2e4", 100);

        connector.Disconnect();

        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move);
    }

    [Test]
    public void ShouldGetNextMoveAsBlack()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();

        var move1 = connector.GetNextMove("", 500);
        var move2 = connector.GetNextMove("e2e4 d7d5", 500);

        connector.Disconnect();

        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move1);
        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move2);
    }

    [Test]
    public void ShouldGetNextMoveAsBlackWithShortMoveTime()
    {
        var connector = new UCIConnector(_setting);
        connector.Connect();

        var move1 = connector.GetNextMove("", 100);
        var move2 = connector.GetNextMove("e2e4 d7d5", 100);

        connector.Disconnect();

        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move1);
        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move2);
    }

    [Test]
    public void ShouldThrowNotConnectedException()
    {
        var connector = new UCIConnector(_setting);

        Assert.IsFalse(connector.IsConnected);
        Assert.Throws<UCIConnectionException>(() => connector.GetNextMove("d2d4", 500));
        Assert.Throws<UCIConnectionException>(() => connector.ConfigureEngine());
        Assert.Throws<UCIConnectionException>(() => connector.StartGame());
    }

    [Test]
    public void ShouldThrowNotConnectedExceptionWithNoOptionsConfigured()
    {
        var connector = new UCIConnector(new Settings { EnginePath = "stockfish" });

        Assert.Throws<UCIConnectionException>(() => connector.ConfigureEngine());
    }

    [Test]
    public void ShouldConfigureArbitraryUciOptions()
    {
        // MultiPV is a harmless option every recent Stockfish build understands; it just has to be accepted.
        _setting.Options["MultiPV"] = "2";

        var connector = new UCIConnector(_setting);
        connector.Connect();

        var move = connector.GetNextMove("e2e4", 500);

        connector.Disconnect();

        StringAssert.IsMatch(MOVE_REGEX_PATTERN, move);
    }
}