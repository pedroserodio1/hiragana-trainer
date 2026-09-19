namespace HiraganaTrainer.Core;

public enum KanaType
{
    Hiragana,
    Katakana,
}

/// <summary>
/// Row/Column place the character in the gojuon table (e.g. き = row "k", column "i").
/// Used to pick plausible multiple-choice distractors. The standalone ん/ン has
/// Row "single" so it never groups with the な-row (which also uses Row "n").
/// </summary>
public sealed record Kana(string Character, string Romaji, KanaType Type, string Row, string Column);
