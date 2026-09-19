using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using HiraganaTrainer.Core;

namespace HiraganaTrainer.Windows;

public sealed class TrainingWindow : Window, IDisposable
{
    private const int DistractorCount = 3;

    private readonly Plugin plugin;
    private readonly Random random = new();

    private Kana? currentKana;
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
    }

    public void Dispose() { }

    public override void Draw()
    {
        currentKana ??= NextCard();

        ImGui.PushFont(ImGui.GetFont());
        ImGui.SetWindowFontScale(3.0f);
        ImGui.TextUnformatted(currentKana!.Character);
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopFont();

        ImGui.Spacing();

        if (lastAnswerCorrect is { } correct)
        {
            ImGui.TextColored(correct ? new Vector4(0.3f, 0.9f, 0.3f, 1f) : new Vector4(0.9f, 0.3f, 0.3f, 1f),
                correct ? "Correct!" : $"Wrong — it was \"{currentKana.Romaji}\".");

            if (ImGui.Button("Next"))
                currentKana = NextCard();
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

    private void DrawMultipleChoice()
    {
        foreach (var choice in choices)
        {
            if (ImGui.Button(choice.Romaji, new Vector2(80, 0)))
                Answer(choice.Character == currentKana!.Character);

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawTyping()
    {
        ImGui.InputText("##answer", ref typedAnswer, 16);
        ImGui.SameLine();

        if (ImGui.Button("Submit") || ImGui.IsKeyPressed(ImGuiKey.Enter))
            Answer(string.Equals(typedAnswer.Trim(), currentKana!.Romaji, StringComparison.OrdinalIgnoreCase));
    }

    private void Answer(bool correct)
    {
        SrsEngine.RecordAnswer(plugin.CurrentProgress, currentKana!.Character, correct);
        lastAnswerCorrect = correct;
    }

    private Kana NextCard()
    {
        var progress = plugin.CurrentProgress;
        SrsEngine.StartNewTurn(progress);

        var kana = SrsEngine.SelectNextCard(progress, KanaRepository.All, random);
        var distractors = SrsEngine.SelectDistractors(kana, KanaRepository.All, DistractorCount, random);
        choices = SrsEngine.Shuffle([kana, .. distractors], random);

        typedAnswer = string.Empty;
        lastAnswerCorrect = null;

        return kana;
    }
}
