using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Windows;
using System.ComponentModel;

namespace QuestSolver.Solvers;

[Description("Request Clicker")]

internal class RequestSolver : BaseSolver
{
    public override SolverItemType ItemType => SolverItemType.UI;

    protected override void Disable()
    {
        Svc.AddonLifecycle.UnregisterListener(OnAddonRequest);
    }

    protected override void Enable()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Request", OnAddonRequest);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Request", OnAddonRequest);
    }

    static int index = 0;
    private static unsafe void OnAddonRequest(AddonEvent type, AddonArgs args)
    {
        if (type == AddonEvent.PostSetup)
        {
            index = 0;
            return;
        }

        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu");

        if (contextMenu != null && contextMenu->IsVisible)
        {
            Callback.Fire(contextMenu, false, 0, 0, 1021003, 0, 0);
        }
        else if(index < ((AddonRequest*)args.Addon)->EntryCount)
        {
            Callback.Fire((AtkUnitBase*)args.Addon, false, 2, index++, 0, 0);
        }
        else
        {
            Callback.Fire((AtkUnitBase*)args.Addon, true, 0);
        }
    }
}
