using HiraganaTrainer.Core;
using Xunit;

namespace HiraganaTrainer.Core.Tests;

public class KanaRepositoryTests
{
    [Fact]
    public void All_ContainsBaseHiraganaAndKatakana_WithNoDuplicatesPerType()
    {
        var hiragana = KanaRepository.All.Where(k => k.Type == KanaType.Hiragana).ToList();
        var katakana = KanaRepository.All.Where(k => k.Type == KanaType.Katakana).ToList();

        Assert.Equal(46, hiragana.Count);
        Assert.Equal(46, katakana.Count);
        Assert.Equal(hiragana.Count, hiragana.Select(k => k.Character).Distinct().Count());
        Assert.Equal(katakana.Count, katakana.Select(k => k.Character).Distinct().Count());
    }
}
