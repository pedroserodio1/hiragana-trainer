namespace HiraganaTrainer.Core;

/// <summary>
/// Leitner scheduling by turn count, not wall-clock time: nothing accumulates
/// backlog if the player goes days without opening the game.
/// </summary>
public static class SrsEngine
{
    /// <summary>Turns to wait before a card in box N (1-indexed) is eligible again.</summary>
    public static readonly int[] BoxIntervals = [1, 2, 4, 8, 16];
    public const int MaxBox = 5;

    public static void StartNewTurn(CharacterProgress progress) => progress.CurrentTurn++;

    public static bool IsEligible(CharacterProgress progress, Kana kana)
    {
        if (!progress.Cards.TryGetValue(kana.Character, out var state))
            return true;

        var interval = BoxIntervals[state.Box - 1];
        return progress.CurrentTurn - state.LastSeenTurn >= interval;
    }

    public static Kana SelectNextCard(CharacterProgress progress, IReadOnlyList<Kana> pool, Random random)
    {
        if (pool.Count == 0)
            throw new ArgumentException("Pool must not be empty.", nameof(pool));

        var eligible = pool.Where(k => IsEligible(progress, k)).ToList();
        var candidates = eligible.Count > 0 ? eligible : pool;
        return candidates[random.Next(candidates.Count)];
    }

    public static void RecordAnswer(CharacterProgress progress, string character, bool correct)
    {
        var state = progress.GetOrCreateCard(character);

        if (correct)
        {
            state.CorrectCount++;
            state.Box = Math.Min(state.Box + 1, MaxBox);
            progress.CurrentStreak++;
            progress.BestStreak = Math.Max(progress.BestStreak, progress.CurrentStreak);
        }
        else
        {
            state.IncorrectCount++;
            state.Box = 1;
            progress.CurrentStreak = 0;
        }

        state.LastSeenTurn = progress.CurrentTurn;
    }

    /// <summary>Picks plausible wrong answers: same gojuon row/column first, then any other kana of the same type.</summary>
    public static IReadOnlyList<Kana> SelectDistractors(Kana correct, IReadOnlyList<Kana> pool, int count, Random random)
    {
        var sameType = pool.Where(k => k.Character != correct.Character && k.Type == correct.Type).ToList();
        var sameGroup = Shuffle(sameType.Where(k => k.Row == correct.Row || k.Column == correct.Column).ToList(), random);
        var rest = Shuffle(sameType.Except(sameGroup).ToList(), random);

        return sameGroup.Concat(rest).Take(count).ToList();
    }

    public static List<T> Shuffle<T>(IReadOnlyList<T> items, Random random)
    {
        var list = items.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
