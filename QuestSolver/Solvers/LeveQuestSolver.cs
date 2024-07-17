using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using QuestSolver.Windows;
using System.ComponentModel;

namespace QuestSolver.Solvers;

[Description("Leve Solver")]

internal class LeveQuestSolver : BaseSolver
{
    public override SolverItemType ItemType => SolverItemType.Quest;

    public static unsafe ushort LeveId => QuestManager.Instance()->LeveQuests.ToArray().FirstOrDefault(i => i.LeveId != 0).LeveId;

    public override Type[] SubSolvers => [typeof(TalkSolver), typeof(RequestSolver)];

    protected override void Enable()
    {
        Svc.Framework.Update += Framework_Update;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", AddonSelectString);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GuildLeve", AddonGetLeve);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ["JournalResult", "SelectYesno"], OnAddonRequest);
    }

    protected override void Disable()
    {
        Svc.Framework.Update -= Framework_Update;
        Svc.AddonLifecycle.UnregisterListener(AddonSelectString);
        Svc.AddonLifecycle.UnregisterListener(AddonGetLeve);
        Svc.AddonLifecycle.UnregisterListener(OnAddonRequest);
    }

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        if (LeveId != 0)
        {
            unsafe
            {
                var leve = (AtkUnitBase*)Svc.GameGui.GetAddonByName("GuildLeve");
                if (leve != null)
                {
                    leve->FireCloseCallback();
                }
            }
            if (!Available) return;
            FinishLeve();
        }
        else
        {
            if (!Available) return;
            GetLeve();
        }
    }

    private static void FinishLeve()
    {
        var leve = Svc.Data.GetExcelSheet<Leve>()?.GetRow(LeveId);
        if (leve == null) return;

        var start = leve.LevelLevemete.Value;
        if (start == null) return;
        if (MoveHelper.MoveTo(start)) return;

        var tar = TargetHelper.GetInteractableTargets(start)
            .FirstOrDefault(o => o.GetNameplateIconId() is 71245);

        if (tar == null) return;

        TargetHelper.Interact(tar);
    }

    private void GetLeve()
    {
        var leve = UpdateLeve();
        if (leve == null)
        {
            IsEnable = false;
            return;
        }

        var start = leve.GetLeveStartLevel();

        if (start == null) return;
        if (MoveHelper.MoveTo(start)) return;

        var tar = TargetHelper.GetInteractableTargets(start)
            .FirstOrDefault(o => o.GetNameplateIconId() is 71241);

        if (tar == null) return;
        _open = true;
        _leveId = leve.RowId;

        TargetHelper.Interact(tar);
    }

    private uint _leveId;
    private bool _open = false;
    private unsafe void AddonSelectString(AddonEvent type, AddonArgs args)
    {
        Svc.Log.Info("Is Open: " + _open);
        Callback.Fire((AtkUnitBase*)args.Addon, true, _open ? 0 : -1);
    }

    private unsafe void AddonGetLeve(AddonEvent type, AddonArgs args)
    {
        _open = false;
        EventHelper.SendEvent(AgentId.LeveQuest, 0, 3, _leveId);
    }

    private static unsafe void OnAddonRequest(AddonEvent type, AddonArgs args)
    {
        Callback.Fire((AtkUnitBase*)args.Addon, true, 0);
    }
    private static Leve? UpdateLeve()
    {
        unsafe
        {
            if (QuestManager.Instance()->NumLeveAllowances == 0) return null;
        }

        if (!Player.Available) return null;

        var leves = Svc.Data.GetExcelSheet<Leve>();
        if (leves == null) return null;

        var job = Player.Job.ToString();
        var prop = typeof(ClassJobCategory).GetProperty(job);
        if (prop == null) return null;

        var availableLeves = leves
            .Where(l => (bool?)prop.GetValue(l.ClassJobCategory.Value) ?? false) //Class
            .Where(l => Player.Level >= l.ClassJobLevel) //Level
            .Where(QuestHelper.CanTake); //Have items.

        if (!availableLeves.Any()) return null;

        var result = availableLeves.MaxBy(l => l.ClassJobLevel);
        if (result != null)
        {
            Svc.Log.Info("Try to finish Leve " + result.Name.RawString + " " + result.RowId);
        }
        return result;
    }
}
