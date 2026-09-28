using Chess.Uci.Connector;

namespace Chess.Battle;

public class UCIBattleConfiguration
{
    public UCIConnector UCI { get; set; }
    public string Name { get; set; }
}