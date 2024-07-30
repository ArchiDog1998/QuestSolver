using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using QuestSolver.Windows;
using System.ComponentModel;
using System.Numerics;

namespace QuestSolver.Solvers;

[Description("Leve Solver")]
internal class LeveQuestSolver : BaseSolver
{
    public override string Description => "Only Craft and gather leves are available, maybe!";

    public override SolverItemType ItemType => SolverItemType.Quest;

    public static unsafe LeveWork LeveWork => QuestManager.Instance()->LeveQuests.ToArray().FirstOrDefault(i => i.LeveId != 0);

    public override Type[] SubSolvers => [typeof(TalkSolver), typeof(RequestSolver)];

    protected override void Enable()
    {
        Svc.Framework.Update += Framework_Update;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", AddonSelectString);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GuildLeve", AddonGetLeve);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GuildLeveDifficulty", AddonGetLeveDifficulty);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, ["JournalResult", "SelectYesno"], OnAddonRequest);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "Gathering", OnAddonGathering);
    }

    private unsafe void AddonGetLeveDifficulty(AddonEvent type, AddonArgs args)
    {
        Callback.Fire((AtkUnitBase*)args.Addon, true, 0, 0);
    }

    private unsafe void OnAddonGathering(AddonEvent type, AddonArgs args)
    {
        var gather = (AddonGathering*)args.Addon;

        var index = 0;
        foreach (var item in gather->GatheredItemComponentCheckbox)
        {
            if (item.Value->IsEnabled)
            {
                CallbackHelper.Fire((AtkUnitBase*)args.Addon, true, index);
                break;
            }
            index++;
        }
    }

    protected override void Disable()
    {
        Svc.Framework.Update -= Framework_Update;
        Svc.AddonLifecycle.UnregisterListener(AddonSelectString);
        Svc.AddonLifecycle.UnregisterListener(AddonGetLeveDifficulty);
        Svc.AddonLifecycle.UnregisterListener(AddonGetLeve);
        Svc.AddonLifecycle.UnregisterListener(OnAddonRequest);
        Svc.AddonLifecycle.UnregisterListener(OnAddonGathering);
    }

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        if (LeveWork.LeveId != 0)
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
        var leve = Svc.Data.GetExcelSheet<Leve>()?.GetRow(LeveWork.LeveId);
        if (leve == null) return;

        if (LeveWork.Sequence == 255)
        {
            var end = leve.LevelLevemete.Value;
            if (end == null) return;
            if (MoveHelper.MoveTo(end)) return;

            var tar = TargetHelper.GetInteractableTargets(end)
                .FirstOrDefault(o => o.GetNameplateIconId() is 71245);

            if (tar == null) return;

            TargetHelper.Interact(tar);
        }
        else if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty])
        {
            var start = leve.LevelStart.Value;
            FinishEventDuty(start);
        }
        else
        {
            var start = leve.LevelStart.Value;
            if (start == null) return;
            if (MoveHelper.MoveTo(start)) return;
            InitEventDuty();
        }
    }

    private static void FinishEventDuty(Level? start)
    {
        var tar = Svc.Objects.Where(obj => obj.IsTargetable && obj.IsValid() 
            && obj.GetNameplateIconId() is 71244)
            .MinBy(i => Vector3.DistanceSquared(i.Position, Player.Object.Position));

        if (tar == null)
        {
            if (start == null) return;
            if (MoveHelper.MoveTo(start)) return;
            return;
        }

        if (MoveHelper.MoveTo(tar.Position, 0)) return;

        TargetHelper.Interact(tar);
    }

    private static DateTime _lastClick = DateTime.Now;
    private static unsafe void InitEventDuty()
    {
        if (DateTime.Now - _lastClick < TimeSpan.FromSeconds(0.3f)) return;
        _lastClick = DateTime.Now;

        var journal = (AtkUnitBase*)Svc.GameGui.GetAddonByName("JournalDetail");
        if (journal == null)
        {
            UIModule.Instance()->ExecuteMainCommand(4);
        }
        else if(Svc.GameGui.GetAddonByName("SelectYesno") == IntPtr.Zero
            && Svc.GameGui.GetAddonByName("GuildLeveDifficulty") == IntPtr.Zero)
        {
            Callback.Fire(journal, true, 4, (uint)LeveWork.LeveId);
        }
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
        var open = _open;
        if (Svc.Targets.Target?.GetNameplateIconId() is 71245)
        {
            open = true;
        }
        Svc.Log.Info("Is Open: " + open);
        Callback.Fire((AtkUnitBase*)args.Addon, true, open ? 0 : -1);
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
