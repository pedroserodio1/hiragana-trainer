using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using HiraganaTrainer.Core;

namespace HiraganaTrainer.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin)
        : base("Hiragana Trainer Settings##HiraganaTrainer_ConfigWindow")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(320, 220);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = plugin.Configuration;

        var autoTrigger = config.AutoTriggerEnabled;
        if (ImGui.Checkbox("Enable automatic popup", ref autoTrigger))
        {
            config.AutoTriggerEnabled = autoTrigger;
            config.Save();
        }

        using (ImRaii.Disabled(!config.AutoTriggerEnabled))
        {
            var onDutyQueue = config.TriggerOnDutyQueue;
            if (ImGui.Checkbox("Trigger while queuing for a duty", ref onDutyQueue))
            {
                config.TriggerOnDutyQueue = onDutyQueue;
                config.Save();
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Opens when you register for a duty, and closes itself as soon as a duty is found.");

            var cooldown = config.AutoTriggerCooldownMinutes;
            if (ImGui.SliderInt("Cooldown (minutes)", ref cooldown, 1, 60))
            {
                config.AutoTriggerCooldownMinutes = cooldown;
                config.Save();
            }
        }

        ImGui.Spacing();

        var modeIndex = (int)config.DefaultAnswerMode;
        if (ImGui.Combo("Answer mode", ref modeIndex, "Multiple choice\0Typing\0"))
        {
            config.DefaultAnswerMode = (AnswerMode)modeIndex;
            config.Save();
        }

        ImGui.Spacing();

        var unlockThreshold = config.NewKanaUnlockThreshold;
        if (ImGui.SliderInt("New kana pace", ref unlockThreshold, 1, SrsEngine.MasteryCorrectThreshold))
        {
            config.NewKanaUnlockThreshold = unlockThreshold;
            config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Correct answers needed on every kana in the current row before the\n"
                + $"next row unlocks. Low = rows unlock quickly. {SrsEngine.MasteryCorrectThreshold} = wait until fully mastered.");
        }
    }
}
