namespace HiraganaTrainer.Core;

public enum CardPresentation
{
    /// <summary>A kana the player has never seen: reveal it with its answer, no quiz yet.</summary>
    Teach,

    /// <summary>A kana the player has seen before: ask them to answer it.</summary>
    Quiz,
}

public sealed record NextCard(Kana Kana, CardPresentation Presentation);

/// <summary>
/// Leitner scheduling by turn count, not wall-clock time: nothing accumulates
/// backlog if the player goes days without opening the game. Kana are introduced
/// gojuon row by row (per script) instead of all at once, and within what's
/// eligible for review, cards the player struggles with are picked more often.
/// </summary>
public static class SrsEngine
{
    /// <summary>Turns to wait before a card in box N (1-indexed) is eligible again.</summary>
    public static readonly int[] BoxIntervals = [1, 2, 4, 8, 16];
    public const int MaxBox = 5;

    /// <summary>Box a card must reach before it counts as "mastered enough" to unlock the next row.</summary>
    public const int MasteryBoxThreshold = 2;

    public static void StartNewTurn(CharacterProgress progress) => progress.CurrentTurn++;

    public static IReadOnlyList<Kana> GetUnlockedPool(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type)
    {
        var unlockedRows = KanaRepository.RowOrder.Take(progress.GetUnlockedGroups(type)).ToHashSet();
        return allKana.Where(k => k.Type == type && unlockedRows.Contains(k.Row)).ToList();
    }

    public static void MaybeUnlockNextGroup(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type)
    {
        var unlocked = progress.GetUnlockedGroups(type);
        if (unlocked >= KanaRepository.RowOrder.Count) return;

        var pool = GetUnlockedPool(progress, allKana, type);
        var mastered = pool.All(k => progress.Cards.TryGetValue(k.Character, out var s) && s.Box >= MasteryBoxThreshold);
        if (mastered) progress.SetUnlockedGroups(type, unlocked + 1);
    }

    public static bool IsEligible(CharacterProgress progress, Kana kana)
    {
        if (!progress.Cards.TryGetValue(kana.Character, out var state))
            return true;

        var interval = BoxIntervals[state.Box - 1];
        return progress.CurrentTurn - state.LastSeenTurn >= interval;
    }

    public static NextCard SelectNextCard(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type, Random random)
    {
        MaybeUnlockNextGroup(progress, allKana, type);
        var pool = GetUnlockedPool(progress, allKana, type);
        if (pool.Count == 0)
            throw new ArgumentException("No kana unlocked for this type.", nameof(allKana));

        var unseen = pool.Where(k => !progress.Cards.ContainsKey(k.Character)).ToList();
        if (unseen.Count > 0)
            return new NextCard(unseen[random.Next(unseen.Count)], CardPresentation.Teach);

        var eligible = pool.Where(k => IsEligible(progress, k)).ToList();
        var candidates = eligible.Count > 0 ? eligible : pool;
        return new NextCard(WeightedPick(progress, candidates, random), CardPresentation.Quiz);
    }

    /// <summary>Marks a never-seen kana as introduced, without counting it as a right/wrong answer.</summary>
    public static void RecordTeach(CharacterProgress progress, string character)
    {
        var state = progress.GetOrCreateCard(character);
        state.LastSeenTurn = progress.CurrentTurn;
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

    /// <summary>Picks plausible wrong answers: same gojuon row/column first, then any other kana in the pool.</summary>
    public static IReadOnlyList<Kana> SelectDistractors(Kana correct, IReadOnlyList<Kana> pool, int count, Random random)
    {
        var others = pool.Where(k => k.Character != correct.Character).ToList();
        var sameGroup = Shuffle(others.Where(k => k.Row == correct.Row || k.Column == correct.Column).ToList(), random);
        var rest = Shuffle(others.Except(sameGroup).ToList(), random);

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

    /// <summary>Weight favors cards in lower boxes and with more past mistakes — never seen is handled separately.</summary>
    private static int Weight(CharacterProgress progress, Kana kana)
    {
        if (!progress.Cards.TryGetValue(kana.Character, out var state))
            return 1;

        return Math.Max(1, (MaxBox - state.Box + 1) + state.IncorrectCount);
    }

    private static Kana WeightedPick(CharacterProgress progress, IReadOnlyList<Kana> candidates, Random random)
    {
        var weights = candidates.Select(k => Weight(progress, k)).ToArray();
        var roll = random.Next(weights.Sum());

        var cumulative = 0;
        for (var i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return candidates[i];
        }

        return candidates[^1];
    }
}
