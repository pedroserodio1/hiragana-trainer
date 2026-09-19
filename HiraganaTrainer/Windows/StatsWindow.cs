using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using HiraganaTrainer.Core;

namespace HiraganaTrainer.Windows;

public sealed class StatsWindow : Window, IDisposable
{
    private enum Filter { All, Hiragana, Katakana }

    private readonly Plugin plugin;
    private Filter filter = Filter.All;

    public StatsWindow(Plugin plugin)
        : base("Hiragana Trainer Stats##HiraganaTrainer_StatsWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var progress = plugin.CurrentProgress;
        ImGui.TextUnformatted($"Current streak: {progress.CurrentStreak}   Best streak: {progress.BestStreak}");
        ImGui.Spacing();

        DrawFilterRadio("All", Filter.All);
        ImGui.SameLine();
        DrawFilterRadio("Hiragana", Filter.Hiragana);
        ImGui.SameLine();
        DrawFilterRadio("Katakana", Filter.Katakana);

        ImGui.Spacing();

        if (!ImGui.BeginTable("##HiraganaTrainer_StatsTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;

        ImGui.TableSetupColumn("Kana");
        ImGui.TableSetupColumn("Romaji");
        ImGui.TableSetupColumn("Correct");
        ImGui.TableSetupColumn("Wrong");
        ImGui.TableSetupColumn("Box");
        ImGui.TableSetupColumn("Mastery");
        ImGui.TableHeadersRow();

        foreach (var kana in KanaRepository.All.Where(Matches))
        {
            progress.Cards.TryGetValue(kana.Character, out var state);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(kana.Character);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(kana.Romaji);
            ImGui.TableNextColumn(); ImGui.TextUnformatted((state?.CorrectCount ?? 0).ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted((state?.IncorrectCount ?? 0).ToString());
            ImGui.TableNextColumn(); ImGui.TextUnformatted((state?.Box ?? 1).ToString());
            ImGui.TableNextColumn(); MasteryStars.Draw(state?.CorrectCount ?? 0);
        }

        ImGui.EndTable();
    }

    private void DrawFilterRadio(string label, Filter value)
    {
        if (ImGui.RadioButton(label, filter == value))
            filter = value;
    }

    private bool Matches(Kana kana) => filter switch
    {
        Filter.Hiragana => kana.Type == KanaType.Hiragana,
        Filter.Katakana => kana.Type == KanaType.Katakana,
        _ => true,
    };
}
