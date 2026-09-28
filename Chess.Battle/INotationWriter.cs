namespace Chess.Battle;

public interface INotationWriter
{
    Chessboard Board { get; set; }
    void WriteMove(string move, MoveResult moveResult);
    void WriteHeader(UCIBattleConfiguration white, UCIBattleConfiguration black);
    string GetResult();
}