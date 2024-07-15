using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Data;
using QuestSolver.Helpers;
using System.ComponentModel;
using XIVConfigUI.Attributes;

namespace QuestSolver.Solvers;

[Description("Yes Solver")]
internal class YesOrNoSolver : BaseSolver
{
    public override uint Icon => 45;

    protected BoolDelay _delay;

    [UI("Delay", Order = 1)]
    public float Delay { get; set; } = 3;

    public YesOrNoSolver()
    {
        _delay = new(() => Delay);
    }

    protected override void Enable()
    {
        Svc.Framework.Update += FrameworkUpdate;
    }

    protected override void Disable()
    {
        Svc.Framework.Update -= FrameworkUpdate;
    }

    private unsafe void FrameworkUpdate(IFramework framework)
    {
        if (MountHelper.InCombat) return;

        var yesOrNo = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno");

        if (yesOrNo == null) return;

        if (_delay.Delay(yesOrNo->IsVisible))
        {
            if (CallbackHelper.Fire(yesOrNo, true, 0))
            {
                Svc.Log.Info("Click Yes");
            }
        }
    }
}
