using HiraganaTrainer.Core;
using Xunit;

namespace HiraganaTrainer.Core.Tests;

public class SrsEngineTests
{
    private static readonly Kana A = new("あ", "a", KanaType.Hiragana, "a", "a");
    private static readonly Kana Ka = new("か", "ka", KanaType.Hiragana, "k", "a");
    private static readonly Kana Ki = new("き", "ki", KanaType.Hiragana, "k", "i");
    private static readonly Kana Sa = new("さ", "sa", KanaType.Hiragana, "s", "a");

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
    public void SelectNextCard_PrefersEligibleCardsOverNonEligible()
    {
        var progress = new CharacterProgress();
        SrsEngine.RecordAnswer(progress, A.Character, correct: true); // box 2, not eligible for a while
        var pool = new[] { A, Ka };

        var picked = SrsEngine.SelectNextCard(progress, pool, new Random(1));

        Assert.Equal(Ka, picked);
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
}
