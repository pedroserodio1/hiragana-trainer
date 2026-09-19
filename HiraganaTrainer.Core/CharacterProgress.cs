namespace HiraganaTrainer.Core;

/// <summary>SRS progress for a single FFXIV character (keyed externally by Content ID).</summary>
public sealed class CharacterProgress
{
    public int CurrentTurn { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public Dictionary<string, SrsCardState> Cards { get; set; } = new();

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
