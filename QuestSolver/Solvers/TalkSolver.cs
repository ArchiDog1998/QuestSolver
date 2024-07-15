using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Data;
using QuestSolver.Helpers;
using System.ComponentModel;
using XIVConfigUI.Attributes;

namespace QuestSolver.Solvers;

[Description("Talk Clicker")]
internal class TalkSolver : BaseSolver
{
    protected BoolDelay _talkDelay;
    protected BoolDelay _cutSceneTalkDelay;

    [Range(1, 1000, ConfigUnitType.None, 0.1f)]
    [UI("Character per second", Order = 1)]
    public float TalkDelay { get => Plugin.Settings.TalkSolverTalkDelay; set => Plugin.Settings.TalkSolverTalkDelay = value; }

    [Range(0, 100, ConfigUnitType.Seconds)]
    [UI("CutScene Talk Delay", Order = 1)]
    public float CutSceneTalkDelay { get => Plugin.Settings.TalkSolverCutSceneTalkDelay; set => Plugin.Settings.TalkSolverCutSceneTalkDelay = value; }

    public override uint Icon => 45;

    public TalkSolver()
    {
        _talkDelay = new(() => TalkDelay);
        _cutSceneTalkDelay = new(() => CutSceneTalkDelay);
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
        var talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Talk");

        if (_talkDelay.Delay(talk->IsVisible, ((AddonTalk*)talk)->AtkTextNode228->NodeText.ToString().Length / TalkDelay))
        {
            if (CallbackHelper.Fire(talk, true))
            {
                Svc.Log.Info("Close Talk");
            }
        }

        talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("CutSceneSelectString");

        if (talk == null) return;

        if (_cutSceneTalkDelay.Delay(talk->IsVisible))
        {
            if (CallbackHelper.Fire(talk, true, 0))
            {
                Svc.Log.Info("Close CutScene Talk");
            }
        }
    }
}
