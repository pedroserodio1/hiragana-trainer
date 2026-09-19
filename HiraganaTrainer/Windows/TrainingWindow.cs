using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using HiraganaTrainer.Core;

namespace HiraganaTrainer.Windows;

public sealed class TrainingWindow : Window, IDisposable
{
    private const int DistractorCount = 3;
    private const float KanaFontSizeMultiplier = 3.0f;

    private readonly Plugin plugin;
    private readonly Random random = new();
    private readonly IFontHandle kanaFont;

    private KanaType? selectedType;
    private KanaType pendingType = KanaType.Hiragana;

    private NextCard? current;
    private List<Kana> choices = [];
    private string typedAnswer = string.Empty;
    private bool? lastAnswerCorrect;

    public TrainingWindow(Plugin plugin)
        : base("Hiragana Trainer##HiraganaTrainer_TrainingWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;

        // A real bigger font, not a scaled-up (blurry) one, built from the same
        // font Dalamud already uses for Japanese glyphs (game text, item names, etc).
        var defaultSpec = (SingleFontSpec)Plugin.PluginInterface.UiBuilder.DefaultFontSpec;
        var bigSpec = defaultSpec with { SizePx = defaultSpec.SizePx * KanaFontSizeMultiplier };
        kanaFont = bigSpec.CreateFontHandle(Plugin.PluginInterface.UiBuilder.FontAtlas);
    }

    public void Dispose() => kanaFont.Dispose();

    public override void Draw()
    {
        if (selectedType is null)
        {
            DrawTypeSelection();
            return;
        }

        current ??= NextCard();

        if (ImGui.SmallButton("Change script"))
        {
            selectedType = null;
            current = null;
            return;
        }

        ImGui.Spacing();

        using (kanaFont.Push())
            ImGui.TextUnformatted(current.Kana.Character);

        ImGui.Spacing();

        if (current.Presentation == CardPresentation.Teach)
        {
            DrawTeach();
        }
        else if (lastAnswerCorrect is { } correct)
        {
            DrawFeedback(correct);
        }
        else if (plugin.Configuration.DefaultAnswerMode == AnswerMode.MultipleChoice)
        {
            DrawMultipleChoice();
        }
        else
        {
            DrawTyping();
        }

        ImGui.Spacing();
        ImGui.TextDisabled($"Streak: {plugin.CurrentProgress.CurrentStreak}  Best: {plugin.CurrentProgress.BestStreak}");
    }

    private void DrawTypeSelection()
    {
        ImGui.TextUnformatted("What do you want to train?");
        ImGui.Spacing();

        if (ImGui.RadioButton("Hiragana", pendingType == KanaType.Hiragana))
            pendingType = KanaType.Hiragana;

        ImGui.SameLine();
        if (ImGui.RadioButton("Katakana", pendingType == KanaType.Katakana))
            pendingType = KanaType.Katakana;

        ImGui.Spacing();
        if (ImGui.Button("Start"))
            selectedType = pendingType;
    }

    private void DrawTeach()
    {
        ImGui.TextUnformatted($"This is \"{current!.Kana.Romaji}\".");

        if (ImGui.Button("Got it"))
        {
            SrsEngine.RecordTeach(plugin.CurrentProgress, current.Kana.Character);
            current = NextCard();
        }
    }

    private void DrawFeedback(bool correct)
    {
        ImGui.TextColored(correct ? new Vector4(0.3f, 0.9f, 0.3f, 1f) : new Vector4(0.9f, 0.3f, 0.3f, 1f),
            correct ? "Correct!" : $"Wrong — it was \"{current!.Kana.Romaji}\".");

        if (ImGui.Button("Next"))
            current = NextCard();
    }

    private void DrawMultipleChoice()
    {
        foreach (var choice in choices)
        {
            if (ImGui.Button(choice.Romaji, new Vector2(80, 0)))
                Answer(choice.Character == current!.Kana.Character);

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawTyping()
    {
        ImGui.InputText("##answer", ref typedAnswer, 16);
        ImGui.SameLine();

        if (ImGui.Button("Submit") || ImGui.IsKeyPressed(ImGuiKey.Enter))
            Answer(string.Equals(typedAnswer.Trim(), current!.Kana.Romaji, StringComparison.OrdinalIgnoreCase));
    }

    private void Answer(bool correct)
    {
        SrsEngine.RecordAnswer(plugin.CurrentProgress, current!.Kana.Character, correct);
        lastAnswerCorrect = correct;
    }

    private NextCard NextCard()
    {
        var progress = plugin.CurrentProgress;
        SrsEngine.StartNewTurn(progress);

        var next = SrsEngine.SelectNextCard(progress, KanaRepository.All, selectedType!.Value, random);

        if (next.Presentation == CardPresentation.Quiz)
        {
            var seenPool = SrsEngine.GetSeenPool(progress, KanaRepository.All, selectedType.Value);
            var distractors = SrsEngine.SelectDistractors(next.Kana, seenPool, DistractorCount, random);
            choices = SrsEngine.Shuffle([next.Kana, .. distractors], random);
        }
        else
        {
            choices = [];
        }

        typedAnswer = string.Empty;
        lastAnswerCorrect = null;

        return next;
    }
}
