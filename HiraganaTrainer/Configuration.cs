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
    /// <summary>Open automatically when you register for a duty queue — closes again as soon as a
    /// duty is found, so it never blocks the confirm dialog's response timer.</summary>
    public bool TriggerOnDutyQueue { get; set; } = true;
    public AnswerMode DefaultAnswerMode { get; set; } = AnswerMode.MultipleChoice;

    /// <summary>Correct answers needed on every kana in the current row before the next row
    /// unlocks — same scale as the mastery stars (15 = fully mastered). Higher = slower pace.</summary>
    public int NewKanaUnlockThreshold { get; set; } = Core.SrsEngine.DefaultUnlockThreshold;

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
