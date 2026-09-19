namespace HiraganaTrainer.Core;

/// <summary>SRS progress for a single FFXIV character (keyed externally by Content ID).</summary>
public sealed class CharacterProgress
{
    public int CurrentTurn { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    /// <summary>How many kana are unlocked per script, in <see cref="KanaRepository"/>'s learning
    /// order. Kana unlock one at a time, not row by row, starting at 1.</summary>
    public Dictionary<KanaType, int> UnlockedCount { get; set; } = new();

    public Dictionary<string, SrsCardState> Cards { get; set; } = new();

    public int GetUnlockedCount(KanaType type) => UnlockedCount.TryGetValue(type, out var value) ? value : 1;

    public void SetUnlockedCount(KanaType type, int value) => UnlockedCount[type] = value;

    public SrsCardState GetOrCreateCard(string character)
    {
        if (!Cards.TryGetValue(character, out var state))
        {
            state = new SrsCardState();
            Cards[character] = state;
        }

        return state;
    }
}
