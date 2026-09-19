namespace HiraganaTrainer.Core;

public sealed class SrsCardState
{
    public int Box { get; set; } = 1;
    public int LastSeenTurn { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
}
