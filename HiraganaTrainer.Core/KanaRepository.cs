using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiraganaTrainer.Core;

public static class KanaRepository
{
    private const string ResourceName = "HiraganaTrainer.Core.Data.kana.json";

    /// <summary>Base gojuon order, same order kana.json is authored in. Kana unlock one at a
    /// time in this order (per script), so this list also doubles as the learning order.</summary>
    public static readonly IReadOnlyList<Kana> All = Load();

    private static IReadOnlyList<Kana> Load()
    {
        var assembly = typeof(KanaRepository).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");

        var entries = JsonSerializer.Deserialize<List<KanaJson>>(stream)
            ?? throw new InvalidOperationException("kana.json is empty or invalid.");

        return entries
            .Select(e => new Kana(e.Character, e.Romaji, Enum.Parse<KanaType>(e.Type, ignoreCase: true), e.Row, e.Column))
            .ToList();
    }

    private sealed record KanaJson(
        [property: JsonPropertyName("character")] string Character,
        [property: JsonPropertyName("romaji")] string Romaji,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("row")] string Row,
        [property: JsonPropertyName("column")] string Column);
}
