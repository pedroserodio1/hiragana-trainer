namespace HiraganaTrainer.Core;

/// <summary>SRS progress for a single FFXIV character (keyed externally by Content ID).</summary>
public sealed class CharacterProgress
{
    public int CurrentTurn { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    /// <summary>Characters unlocked so far per script. Seeded with the vowel row (plus the
    /// standalone ん/ン) the first time a script is touched; from there, each gojuon column
    /// (a-ka-sa-ta..., i-ki-shi-chi..., ...) advances independently as its current kana is
    /// answered correctly enough times.</summary>
    public Dictionary<KanaType, HashSet<string>> UnlockedCharacters { get; set; } = new();

    public Dictionary<string, SrsCardState> Cards { get; set; } = new();

    public HashSet<string> GetUnlockedCharacters(KanaType type)
    {
        if (!UnlockedCharacters.TryGetValue(type, out var set))
        {
            set = [];
            UnlockedCharacters[type] = set;
        }

        return set;
    }

    public void ClearUnlockedCharacters(KanaType type) => UnlockedCharacters[type] = [];

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
