using Dalamud.Configuration;
using ECommons.DalamudServices;
using XIVConfigUI.Attributes;

namespace QuestSolver.Configuration;

internal class Settings : IPluginConfiguration
{
    [Range(0.1f, 10, ConfigUnitType.Yalms)]
    [UI("Moving Distance")]
    public float Distance { get; set; } = 3;
    public int Version { get; set; } = 1;
    public uint MountId { get; set; } = 0;
    public float QuestGetterRange { get; set; } = 10;
    public float TalkSolverTalkDelay { get; set; } = 10;
    public float TalkSolverCutSceneTalkDelay { get; set; } = 3;

    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }
}
