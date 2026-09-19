using Dalamud.Configuration;
using Dalamud.Plugin;
using HiraganaTrainer.Core;

namespace HiraganaTrainer;

public enum AnswerMode
{
    MultipleChoice,
    Typing,
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Global settings — same for every character (confirmed default: everything configurable).
    public bool AutoTriggerEnabled { get; set; } = true;
    public int AutoTriggerCooldownMinutes { get; set; } = 15;
    public bool TriggerOnLoadingScreen { get; set; } = true;
    public bool TriggerOnDutyPop { get; set; } = true;
    public AnswerMode DefaultAnswerMode { get; set; } = AnswerMode.MultipleChoice;

    // Per-character SRS progress, keyed by IClientState.LocalContentId.
    public Dictionary<ulong, CharacterProgress> Characters { get; set; } = new();

    [NonSerialized]
    private IDalamudPluginInterface pluginInterface = null!;

    public void Initialize(IDalamudPluginInterface pi) => pluginInterface = pi;

    public void Save() => pluginInterface.SavePluginConfig(this);

    public CharacterProgress GetProgress(ulong contentId)
    {
        if (!Characters.TryGetValue(contentId, out var progress))
        {
            progress = new CharacterProgress();
            Characters[contentId] = progress;
        }

        return progress;
    }
}
