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
            var onLoadingScreen = config.TriggerOnLoadingScreen;
            if (ImGui.Checkbox("Trigger on loading screens", ref onLoadingScreen))
            {
                config.TriggerOnLoadingScreen = onLoadingScreen;
                config.Save();
            }

            var onDutyPop = config.TriggerOnDutyPop;
            if (ImGui.Checkbox("Trigger on duty pop", ref onDutyPop))
            {
                config.TriggerOnDutyPop = onDutyPop;
                config.Save();
            }

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
            ImGui.SetTooltip("Correct answers needed on a kana before the next one unlocks.\n"
                + $"Low = new kana appear quickly. {SrsEngine.MasteryCorrectThreshold} = wait until it's fully mastered.");
        }
    }
}
