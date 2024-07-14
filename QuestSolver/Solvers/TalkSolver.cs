using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Data;
using System.ComponentModel;
using XIVConfigUI.Attributes;

namespace QuestSolver.Solvers;

[Description("Talk Clicker")]
internal class TalkSolver : BaseSolver
{
    protected BoolDelay _talkDelay;
    protected BoolDelay _cutSceneTalkDelay;


    [UI("Talk Delay", Order = 1)]
    public float TalkDelay { get; set; } = 3;

    [UI("CutScene Talk Delay", Order = 1)]
    public float CutSceneTalkDelay { get; set; } = 3;

    public override uint Icon => 45;

    public TalkSolver()
    {
        _talkDelay = new(() => TalkDelay);
        _cutSceneTalkDelay = new(() => CutSceneTalkDelay);
    }

    public override void Enable()
    {
        Svc.Framework.Update += FrameworkUpdate;
    }

    public override void Disable()
    {
        Svc.Framework.Update -= FrameworkUpdate;
    }

    private unsafe void FrameworkUpdate(IFramework framework)
    {
        var talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Talk");

        if (_talkDelay.Delay(talk->IsVisible))
        {
            Svc.Log.Info("Close Talk");
            Callback.Fire(talk, true);
        }

        talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("CutSceneSelectString");

        if (talk == null) return;

        if (_cutSceneTalkDelay.Delay(talk->IsVisible))
        {
            Svc.Log.Info("Close CutScene Talk");
            Callback.Fire(talk, true, 0);
        }
    }
}
