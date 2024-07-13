using Dalamud.Configuration;
using ECommons.DalamudServices;

namespace QuestSolver.Configuration;

internal class Settings : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }
}
