using HiraganaTrainer.Core;
using Xunit;

namespace HiraganaTrainer.Core.Tests;

public class SrsEngineTests
{
    private static readonly Kana A = new("あ", "a", KanaType.Hiragana, "a", "a");
    private static readonly Kana I = new("い", "i", KanaType.Hiragana, "a", "i");
    private static readonly Kana N = new("ん", "n", KanaType.Hiragana, "single", "n");
    private static readonly Kana Ka = new("か", "ka", KanaType.Hiragana, "k", "a");
    private static readonly Kana Ki = new("き", "ki", KanaType.Hiragana, "k", "i");
    private static readonly Kana Sa = new("さ", "sa", KanaType.Hiragana, "s", "a");
    private static readonly Kana Se = new("せ", "se", KanaType.Hiragana, "s", "e");
    private static readonly Kana So = new("そ", "so", KanaType.Hiragana, "s", "o");

    [Fact]
    public void RecordAnswer_Correct_PromotesBoxAndUpdatesStreak()
    {
        var progress = new CharacterProgress();

        SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        var state = progress.Cards[A.Character];
        Assert.Equal(2, state.Box);
        Assert.Equal(1, state.CorrectCount);
        Assert.Equal(0, state.IncorrectCount);
        Assert.Equal(1, progress.CurrentStreak);
        Assert.Equal(1, progress.BestStreak);
    }

    [Fact]
    public void RecordAnswer_Incorrect_ResetsBoxAndStreak()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        SrsEngine.RecordAnswer(progress, A.Character, correct: false);

        var state = progress.Cards[A.Character];
        Assert.Equal(1, state.Box);
        Assert.Equal(1, state.IncorrectCount);
        Assert.Equal(0, progress.CurrentStreak);
        Assert.Equal(2, progress.BestStreak);
    }

    [Fact]
    public void RecordAnswer_Correct_NeverPromotesPastMaxBox()
    {
        var progress = new CharacterProgress();
        for (var i = 0; i < 10; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        Assert.Equal(SrsEngine.MaxBox, progress.Cards[A.Character].Box);
    }

    [Fact]
    public void IsEligible_NeverSeenCard_IsEligible()
    {
        var progress = new CharacterProgress();

        Assert.True(SrsEngine.IsEligible(progress, A));
    }

    [Fact]
    public void IsEligible_RespectsBoxIntervalInTurns()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // -> box 2, interval 2 turns

        SrsEngine.StartNewTurn(progress); // turn 1
        Assert.False(SrsEngine.IsEligible(progress, A));

        SrsEngine.StartNewTurn(progress); // turn 2
        Assert.True(SrsEngine.IsEligible(progress, A));
    }

    [Fact]
    public void SelectNextCard_NeverSeenKana_ReturnsTeachPresentation()
    {
        var progress = new CharacterProgress();

        var next = SrsEngine.SelectNextCard(progress, new[] { A }, KanaType.Hiragana, new Random(1));

        Assert.Equal(A, next.Kana);
        Assert.Equal(CardPresentation.Teach, next.Presentation);
    }

    [Fact]
    public void RecordTeach_MarksCardSeen_SoNextSelectionIsAQuiz()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordTeach(progress, A.Character);

        var next = SrsEngine.SelectNextCard(progress, new[] { A }, KanaType.Hiragana, new Random(1));

        Assert.Equal(CardPresentation.Quiz, next.Presentation);
    }

    [Fact]
    public void SelectNextCard_PrefersEligibleCardsOverNonEligible()
    {
        var progress = new CharacterProgress(); // A and I share row "a", unlocked together from the start
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordTeach(progress, I.Character);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // -> box 2, interval 2 turns

        SrsEngine.StartNewTurn(progress); // turn 1: I (box 1) is eligible again, A (box 2) is not

        var next = SrsEngine.SelectNextCard(progress, new[] { A, I }, KanaType.Hiragana, new Random(1));

        Assert.Equal(I, next.Kana);
        Assert.Equal(CardPresentation.Quiz, next.Presentation);
    }

    [Fact]
    public void SelectNextCard_FavorsCardsWithMoreMistakes_WhenBothEligible()
    {
        var progress = new CharacterProgress(); // A and I share row "a", unlocked together from the start
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordTeach(progress, I.Character);
        for (var i = 0; i < 8; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: false); // stays box 1, incorrectCount 8

        var pool = new[] { A, I };
        var random = new Random(42);
        var pickedA = 0;

        for (var i = 0; i < 200; i++)
        {
            SrsEngine.StartNewTurn(progress);
            if (SrsEngine.SelectNextCard(progress, pool, KanaType.Hiragana, random).Kana == A)
                pickedA++;
        }

        Assert.True(pickedA > 120, $"Expected the frequently-missed kana to be picked clearly more than half the time, got {pickedA}/200.");
    }

    [Fact]
    public void AdvanceUnlocks_SeedsOnlyTheVowelRow_OnFirstUse()
    {
        var progress = new CharacterProgress();
        var allKana = new[] { A, I, Ka, N };

        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana);

        var pool = SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana);
        Assert.Equal(new[] { A, I }, pool); // か stays locked until unlocked via あ; ん unlocks last, not first
    }

    [Fact]
    public void AdvanceUnlocks_UnlocksStandaloneN_OnlyOnceEveryOtherKanaIsUnlocked()
    {
        var progress = new CharacterProgress();
        var allKana = new[] { A, Ka, N }; // column "a" in this pool is just あ → か
        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana); // seeds A only

        Assert.DoesNotContain(N, SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana));

        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // あ reaches the threshold, unlocking か

        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana, unlockThreshold: 2);

        var pool = SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana);
        Assert.Contains(Ka, pool);
        Assert.Contains(N, pool); // か was the last remaining kana in this pool, so ん unlocks too
    }

    [Fact]
    public void AdvanceUnlocks_UnlocksNextInColumn_OnceThresholdReached_IndependentlyPerColumn()
    {
        var progress = new CharacterProgress();
        var allKana = new[] { A, I, Ka, Ki };
        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana); // seeds A (col a), I (col i)

        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordTeach(progress, I.Character);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // A: 2 correct, meets threshold
        SrsEngine.RecordAnswer(progress, I.Character, correct: true); // I: only 1 correct

        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana, unlockThreshold: 2);

        var pool = SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana);
        Assert.Contains(Ka, pool); // あ's column advances: か unlocks
        Assert.DoesNotContain(Ki, pool); // い's column hasn't reached the threshold yet
    }

    [Fact]
    public void AdvanceUnlocks_DefaultThreshold_RequiresSeveralCorrectAnswers_NotJustOne()
    {
        var progress = new CharacterProgress();
        var allKana = new[] { A, Ka };
        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana); // seeds A
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // only 1 correct answer

        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana);

        var pool = SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana);
        Assert.DoesNotContain(Ka, pool);
    }

    [Fact]
    public void AdvanceUnlocks_DoesNothing_WhenColumnHasNoFurtherRowInThePool()
    {
        var progress = new CharacterProgress();
        var allKana = new[] { A, Ka }; // column "a": only rows "a" and "k" are present here
        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana);
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana, unlockThreshold: 2); // unlocks か
        SrsEngine.RecordTeach(progress, Ka.Character);
        SrsEngine.RecordAnswer(progress, Ka.Character, correct: true);
        SrsEngine.RecordAnswer(progress, Ka.Character, correct: true);

        SrsEngine.AdvanceUnlocks(progress, allKana, KanaType.Hiragana, unlockThreshold: 2); // no further row to chain into

        var pool = SrsEngine.GetUnlockedPool(progress, allKana, KanaType.Hiragana);
        Assert.Equal(new[] { A, Ka }, pool);
    }

    [Fact]
    public void GetSeenPool_OnlyIncludesTaughtKana_RegardlessOfRow()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordTeach(progress, Ka.Character);
        SrsEngine.RecordTeach(progress, Se.Character);
        // So was never taught, even though it's in the pool passed in.

        var seen = SrsEngine.GetSeenPool(progress, new[] { A, Ka, Se, So }, KanaType.Hiragana);

        Assert.Equal(new[] { A, Ka, Se }, seen);
    }

    [Fact]
    public void IsMastered_False_BeforeReachingCorrectThreshold()
    {
        var progress = new CharacterProgress();
        for (var i = 0; i < SrsEngine.MasteryCorrectThreshold - 1; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        Assert.False(SrsEngine.IsMastered(progress, A));
    }

    [Fact]
    public void IsMastered_True_OnceCorrectThresholdIsReached()
    {
        var progress = new CharacterProgress();
        for (var i = 0; i < SrsEngine.MasteryCorrectThreshold; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        Assert.True(SrsEngine.IsMastered(progress, A));
    }

    [Fact]
    public void SelectNextCard_ExcludesMasteredCards_EvenWhenTheyWouldOtherwiseBeTheOnlyEligibleOne()
    {
        var progress = new CharacterProgress(); // A and I share row "a", unlocked together from the start
        SrsEngine.RecordTeach(progress, A.Character);
        SrsEngine.RecordTeach(progress, I.Character);
        for (var i = 0; i < SrsEngine.MasteryCorrectThreshold; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: true); // A is now mastered
        SrsEngine.RecordAnswer(progress, I.Character, correct: true); // I: box 2, not mastered

        SrsEngine.StartNewTurn(progress);
        SrsEngine.StartNewTurn(progress); // enough turns for I (box 2, interval 2) to be eligible again

        var next = SrsEngine.SelectNextCard(progress, new[] { A, I }, KanaType.Hiragana, new Random(1));

        Assert.Equal(I, next.Kana);
        Assert.Equal(CardPresentation.Quiz, next.Presentation);
    }

    [Fact]
    public void SelectNextCard_ReturnsMastered_WhenEveryUnlockedCardIsMastered()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordTeach(progress, A.Character);
        for (var i = 0; i < SrsEngine.MasteryCorrectThreshold; i++)
            SrsEngine.RecordAnswer(progress, A.Character, correct: true);

        var next = SrsEngine.SelectNextCard(progress, new[] { A }, KanaType.Hiragana, new Random(1));

        Assert.Equal(CardPresentation.Mastered, next.Presentation);
    }

    [Fact]
    public void ResetProgress_ClearsCardsAndUnlockedCharacters_ForThatTypeOnly()
    {
        var progress = new CharacterProgress();
        SrsEngine.AdvanceUnlocks(progress, new[] { A, I, Ka }, KanaType.Hiragana); // seeds A, I
        SrsEngine.RecordAnswer(progress, A.Character, correct: true);
        SrsEngine.RecordAnswer(progress, I.Character, correct: true);

        SrsEngine.ResetProgress(progress, new[] { A, I, Ka }, KanaType.Hiragana);

        Assert.Empty(progress.GetUnlockedCharacters(KanaType.Hiragana));
        Assert.DoesNotContain(A.Character, progress.Cards.Keys);
        Assert.DoesNotContain(I.Character, progress.Cards.Keys);
    }

    [Fact]
    public void SelectDistractors_NeverIncludesTheCorrectAnswer()
    {
        var pool = new[] { A, Ka, Ki, Sa };

        var distractors = SrsEngine.SelectDistractors(A, pool, count: 3, new Random(1));

        Assert.DoesNotContain(A, distractors);
        Assert.Equal(3, distractors.Count);
    }

    [Fact]
    public void SelectDistractors_PrefersSameRowOrColumn()
    {
        var pool = new[] { A, Ka, Ki, Sa };

        var distractors = SrsEngine.SelectDistractors(Ka, pool, count: 1, new Random(1));

        Assert.Equal(Ki, distractors.Single()); // same row "k", the only same-group option
    }

    [Fact]
    public void SelectDistractors_CanMixKanaFromDifferentRows_WhenPoolSpansRows()
    {
        // Mirrors picking options across everything taught so far (e.g. A, KA, SE, SO),
        // not just the correct answer's own row/column.
        var pool = new[] { A, Ka, Se, So };

        var distractors = SrsEngine.SelectDistractors(A, pool, count: 3, new Random(1));

        Assert.Equal(3, distractors.Count);
        Assert.DoesNotContain(A, distractors);
    }
}
