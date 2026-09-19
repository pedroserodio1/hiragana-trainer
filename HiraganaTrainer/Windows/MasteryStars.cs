using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using HiraganaTrainer.Core;

namespace HiraganaTrainer.Windows;

/// <summary>A row of star icons showing progress toward <see cref="SrsEngine.MasteryCorrectThreshold"/>
/// correct answers, mirroring the star-progress UI in Hiragana Pro.</summary>
internal static class MasteryStars
{
    private const int StarCount = 5;
    public static readonly Vector4 EarnedColor = new(1.0f, 0.85f, 0.2f, 1.0f);
    public static readonly Vector4 UnearnedColor = new(0.4f, 0.4f, 0.4f, 1.0f);

    public static void Draw(int correctCount)
    {
        var correctPerStar = SrsEngine.MasteryCorrectThreshold / (float)StarCount;

        using (Plugin.PluginInterface.UiBuilder.IconFontHandle.Push())
        {
            for (var i = 0; i < StarCount; i++)
            {
                var earned = correctCount >= (i + 1) * correctPerStar;
                ImGui.TextColored(earned ? EarnedColor : UnearnedColor, FontAwesomeIcon.Star.ToIconString());
                if (i < StarCount - 1) ImGui.SameLine(0, 2);
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"{Math.Min(correctCount, SrsEngine.MasteryCorrectThreshold)}/{SrsEngine.MasteryCorrectThreshold} correct");
    }
}
