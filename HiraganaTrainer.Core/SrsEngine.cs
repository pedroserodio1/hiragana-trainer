namespace HiraganaTrainer.Core;

public enum CardPresentation
{
    /// <summary>A kana the player has never seen: reveal it with its answer, no quiz yet.</summary>
    Teach,

    /// <summary>A kana the player has seen before: ask them to answer it.</summary>
    Quiz,

    /// <summary>Every unlocked kana of this script has been mastered — nothing left to review right now.</summary>
    Mastered,
}

public sealed record NextCard(Kana Kana, CardPresentation Presentation);

/// <summary>
/// Leitner scheduling by turn count, not wall-clock time: nothing accumulates
/// backlog if the player goes days without opening the game. The vowel row (plus
/// ん/ン) unlocks all at once as a starting point; from there, each gojuon column
/// (a-ka-sa-ta-na-ha-ma-ya-ra-wa, i-ki-shi-chi-..., ...) advances independently and
/// in parallel — once a kana is answered correctly enough times, the next one down
/// its own column unlocks, regardless of how the other columns are doing. Within
/// what's eligible for review, cards the player struggles with are picked more often.
/// </summary>
public static class SrsEngine
{
    /// <summary>Turns to wait before a card in box N (1-indexed) is eligible again.</summary>
    public static readonly int[] BoxIntervals = [1, 2, 4, 8, 16];
    public const int MaxBox = 5;

    /// <summary>Correct answers a card needs before it graduates out of the review rotation entirely.</summary>
    public const int MasteryCorrectThreshold = 15;

    /// <summary>Default correct answers required on a kana before the next one down its column
    /// unlocks, used when the caller doesn't pass its own (configurable) pace. Same unit as the
    /// mastery stars, so "wait until it's mostly learned" and "wait until it's fully mastered"
    /// are both just a number on the same scale.</summary>
    public const int DefaultUnlockThreshold = 5;

    public static void StartNewTurn(CharacterProgress progress) => progress.CurrentTurn++;

    public static IReadOnlyList<Kana> GetUnlockedPool(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type)
    {
        var unlocked = progress.GetUnlockedCharacters(type);
        return allKana.Where(k => k.Type == type && unlocked.Contains(k.Character)).ToList();
    }

    /// <summary>Kana the player has actually been taught, regardless of which row they belong to —
    /// used as the pool for multiple-choice distractors, so options never include an untaught kana.</summary>
    public static IReadOnlyList<Kana> GetSeenPool(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type) =>
        allKana.Where(k => k.Type == type && progress.Cards.ContainsKey(k.Character)).ToList();

    /// <summary>Wipes all progress for one script (unlocked kana and per-card state), so it starts over
    /// from the vowel row again. The other script and the character's streak are left untouched.</summary>
    public static void ResetProgress(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type)
    {
        foreach (var kana in allKana.Where(k => k.Type == type))
            progress.Cards.Remove(kana.Character);

        progress.ClearUnlockedCharacters(type);
    }

    /// <summary>Seeds the vowel row (+ ん/ン) as unlocked on first use, then advances every unlocked
    /// column independently: a kana that's reached <paramref name="unlockThreshold"/> correct
    /// answers unlocks the next kana down its own column (same <see cref="Kana.Column"/>, next
    /// row in <see cref="KanaRepository.RowOrder"/>).</summary>
    public static void AdvanceUnlocks(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type, int unlockThreshold = DefaultUnlockThreshold)
    {
        var unlocked = progress.GetUnlockedCharacters(type);
        var typeKana = allKana.Where(k => k.Type == type).ToList();

        if (unlocked.Count == 0)
        {
            foreach (var kana in typeKana.Where(k => k.Row == "a" || k.Row == "single"))
                unlocked.Add(kana.Character);
            return;
        }

        foreach (var kana in typeKana.Where(k => unlocked.Contains(k.Character)))
        {
            if (!progress.Cards.TryGetValue(kana.Character, out var state) || state.CorrectCount < unlockThreshold)
                continue;

            var next = NextInColumn(kana, typeKana);
            if (next is not null) unlocked.Add(next.Character);
        }
    }

    /// <summary>The next kana down the same gojuon column (e.g. あ → か → さ → ...), or null if
    /// this column has no further row (e.g. い has no y-row or w-row entry to chain into).</summary>
    private static Kana? NextInColumn(Kana kana, IReadOnlyList<Kana> typeKana)
    {
        var rowIndex = Array.IndexOf(KanaRepository.RowOrder, kana.Row);
        if (rowIndex < 0) return null; // e.g. the standalone ん/ン (Row "single") never chains

        return typeKana
            .Where(k => k.Column == kana.Column)
            .Select(k => (Kana: k, RowIndex: Array.IndexOf(KanaRepository.RowOrder, k.Row)))
            .Where(k => k.RowIndex > rowIndex)
            .OrderBy(k => k.RowIndex)
            .Select(k => k.Kana)
            .FirstOrDefault();
    }

    public static bool IsEligible(CharacterProgress progress, Kana kana)
    {
        if (!progress.Cards.TryGetValue(kana.Character, out var state))
            return true;

        var interval = BoxIntervals[state.Box - 1];
        return progress.CurrentTurn - state.LastSeenTurn >= interval;
    }

    /// <summary>A card that's been answered correctly enough times graduates out of review for good.</summary>
    public static bool IsMastered(CharacterProgress progress, Kana kana) =>
        progress.Cards.TryGetValue(kana.Character, out var state) && state.CorrectCount >= MasteryCorrectThreshold;

    public static NextCard SelectNextCard(CharacterProgress progress, IReadOnlyList<Kana> allKana, KanaType type, Random random, int unlockThreshold = DefaultUnlockThreshold)
    {
        AdvanceUnlocks(progress, allKana, type, unlockThreshold);
        var pool = GetUnlockedPool(progress, allKana, type);
        if (pool.Count == 0)
            throw new ArgumentException("No kana unlocked for this type.", nameof(allKana));

        var unseen = pool.Where(k => !progress.Cards.ContainsKey(k.Character)).ToList();
        if (unseen.Count > 0)
            return new NextCard(unseen[random.Next(unseen.Count)], CardPresentation.Teach);

        var reviewable = pool.Where(k => !IsMastered(progress, k)).ToList();
        if (reviewable.Count == 0)
            return new NextCard(pool[0], CardPresentation.Mastered);

        var eligible = reviewable.Where(k => IsEligible(progress, k)).ToList();
        var candidates = eligible.Count > 0 ? eligible : reviewable;
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

    /// <summary>Picks plausible wrong answers from the given pool (normally "already taught" kana):
    /// same gojuon row/column first, then any other kana in the pool.</summary>
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
