namespace HiraganaTrainer.Core;

/// <summary>SRS progress for a single FFXIV character (keyed externally by Content ID).</summary>
public sealed class CharacterProgress
{
    public int CurrentTurn { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    /// <summary>How many rows of <see cref="KanaRepository.RowOrder"/> are unlocked per script, starting at 1.</summary>
    public Dictionary<KanaType, int> UnlockedGroups { get; set; } = new();

    public Dictionary<string, SrsCardState> Cards { get; set; } = new();

    public int GetUnlockedGroups(KanaType type) => UnlockedGroups.TryGetValue(type, out var value) ? value : 1;

    public void SetUnlockedGroups(KanaType type, int value) => UnlockedGroups[type] = value;

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
