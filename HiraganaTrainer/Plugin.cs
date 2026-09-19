using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HiraganaTrainer.Windows;

namespace HiraganaTrainer;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/kana";
    private const string DutyFoundAddonName = "ContentsFinderConfirm";

    public Configuration Configuration { get; }

    public readonly WindowSystem WindowSystem = new("HiraganaTrainer");
    private TrainingWindow TrainingWindow { get; }
    private ConfigWindow ConfigWindow { get; }
    private StatsWindow StatsWindow { get; }

    private DateTime lastAutoTriggerUtc = DateTime.MinValue;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        TrainingWindow = new TrainingWindow(this);
        ConfigWindow = new ConfigWindow(this);
        StatsWindow = new StatsWindow(this);

        WindowSystem.AddWindow(TrainingWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(StatsWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the kana trainer. Use '/kana config' or '/kana stats' for the other windows.",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Condition.ConditionChange += OnConditionChange;
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, DutyFoundAddonName, OnDutyFoundAddon);
    }

    public void Dispose()
    {
        Condition.ConditionChange -= OnConditionChange;
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, DutyFoundAddonName, OnDutyFoundAddon);

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        TrainingWindow.Dispose();
        ConfigWindow.Dispose();
        StatsWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    /// <summary>Current character's SRS progress, keyed by Content ID. Falls back to a shared
    /// bucket (id 0) when no character is loaded, e.g. at the title screen.</summary>
    public Core.CharacterProgress CurrentProgress => Configuration.GetProgress(PlayerState.IsLoaded ? PlayerState.ContentId : 0);

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "config":
                ConfigWindow.Toggle();
                break;
            case "stats":
                StatsWindow.Toggle();
                break;
            default:
                TrainingWindow.Toggle();
                break;
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void ToggleMainUi() => TrainingWindow.Toggle();

    public void ToggleStatsUi() => StatsWindow.Toggle();

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BetweenAreas || flag == ConditionFlag.BetweenAreas51)
        {
            if (value && Configuration.TriggerOnLoadingScreen)
                TryAutoTrigger();

            return;
        }

        // Registered for a duty (roulette/queue) and actively waiting — dead time worth using.
        if (flag == ConditionFlag.InDutyQueue && value && Configuration.TriggerOnDutyQueue)
            TryAutoTrigger();
    }

    /// <summary>The duty-found confirmation has a short response timer, so the training window
    /// always gets out of the way here, regardless of how it was opened.</summary>
    private void OnDutyFoundAddon(AddonEvent type, AddonArgs args) => TrainingWindow.IsOpen = false;

    private void TryAutoTrigger()
    {
        if (!Configuration.AutoTriggerEnabled) return;

        var cooldown = TimeSpan.FromMinutes(Configuration.AutoTriggerCooldownMinutes);
        if (DateTime.UtcNow - lastAutoTriggerUtc < cooldown) return;

        lastAutoTriggerUtc = DateTime.UtcNow;
        TrainingWindow.IsOpen = true;
    }
}
