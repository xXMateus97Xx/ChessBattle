namespace Chess.Battle;

public class BattleSettings
{
    public EngineSettings Engine1 { get; set; }
    public EngineSettings Engine2 { get; set; }
    public string OutputPath { get; set; }
    public int WhiteEngine { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan MoveTime { get; set; }
}