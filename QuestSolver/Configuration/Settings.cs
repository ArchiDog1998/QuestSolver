using Dalamud.Configuration;
using ECommons.DalamudServices;

namespace QuestSolver.Configuration;

internal class Settings : IPluginConfiguration
{
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
