using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using QuestSolver.Windows;
using System.ComponentModel;
using System.Numerics;

namespace QuestSolver.Solvers;


[Description("Quest Finisher")]
internal class QuestFinishSolver : BaseSolver
{
    public override SolverItemType ItemType => SolverItemType.Quest;
    private readonly List<uint> MovedLevels = [];

    internal QuestItem? QuestItem { get; private set; } =  null;

    public override Type[] SubSolvers => [typeof(TalkSolver), typeof(YesOrNoSolver), typeof(RequestSolver)];

    protected override void Enable()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnAddonJournalResult);
        //Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "JournalResult", OnAddonJournalResult);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnAddonRequest);

        Svc.Framework.Update += FrameworkUpdate;
    }

    private unsafe void FindQuest()
    {
        var result = QuestHelper.FindBestQuest();

        if (result?.Quest.RowId == QuestItem?.Quest.RowId) return;

        Svc.Log.Info("Try to finish " +  result?.Quest.Name.RawString + " " + result?.Quest.RowId);
        QuestItem = result;
        _validTargets.Clear();
    }

    protected override void Disable()
    {
        Svc.AddonLifecycle.UnregisterListener(OnAddonJournalResult);
        Svc.AddonLifecycle.UnregisterListener(OnAddonRequest);

        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;

        MovedLevels.Clear();
        _validTargets.Clear();

        QuestItem = null;
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (!Available) return;
        if (WaitForCombat())
        {
            if (MountHelper.IsMount)
            {
                MountHelper.TryDisMount();
            }
            return;
        }
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty]) return;

        FindQuest();

        if (QuestItem == null)
        {
            IsEnable = false;
            return;
        }

        bool broke = false;
        foreach (var level in QuestItem.Levels)
        {
            if (MovedLevels.Contains(level.RowId)) continue;
            if (CalculateOneLevel(level, QuestItem.Quest))
            {
                MovedLevels.Add(level.RowId);
                _validTargets.Clear();
                Svc.Log.Info("Finished Level " + level.RowId);
            }
            broke = true;
            break;
        }
        if (!broke)
        {
            MovedLevels.Clear();
        }
    }

    private unsafe bool WaitForCombat()
    {
        if (!MountHelper.InCombat) return false;

        foreach (var obj in Svc.Objects)
        {
            if (obj is not IBattleChara) continue;
            if (obj.Struct()->EventId.ContentId is EventHandlerType.Quest) return true;
        }

        return false;
    }

    private readonly HashSet<IGameObject> _validTargets = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True for next.</returns>
    private bool CalculateOneLevel(Level level, QuestWithTodo quest)
    {
        if (level.IsInSide())
        {
            FindValidTargets(level, quest);

            if (_validTargets.Count == 0) return true;

            var obj = _validTargets.MinBy(t => Vector3.DistanceSquared(t.Position, Player.Object.Position))!;

            //Svc.Log.Info(string.Join(", ", quest.sc.ToArray()));

            //Svc.Log.Info("Plan to talk with " + obj.Name + quest.ScriptInstruction[_quest.Work.Sequence].RawString);

           
            if (!MoveHelper.MoveTo(obj.Position, 0))
            {
                //TODO: Emote fix!
                TargetHelper.Interact(obj);
                //_validTargets.Remove(obj); //No Need!
            }
        }
        else
        {
            MoveHelper.MoveTo(level);
        }
        return false;
    }

    private void FindValidTargets(Level level, QuestWithTodo quest)
    {
        var eobjs = Svc.Data.GetExcelSheet<EObj>();

        var objs = Svc.Objects.Union(_validTargets).Where(item => item.IsValid(level)).ToArray();
        _validTargets.Clear();
        foreach (var item in objs)
        {
            var icon = item.GetNameplateIconId();

            if (icon is 71203 or 71205 or 70983//MSQ
                or 71343 or 71345 // Important
                or 71223 or 71225 // Side
                )
            {
                _validTargets.Add(item);
            }
            else if (eobjs?.GetRow(item.DataId)?.Data == quest.RowId)
            {
                _validTargets.Add(item);
            }
#if DEBUG
            else if (icon != 0)
            {
                Svc.Log.Error($"{item.Name} Name Place {icon}");
            }
#endif
        }
    }
    private unsafe void OnAddonJournalResult(AddonEvent type, AddonArgs args)
    {
        var item = QuestItem?.Quest.OptionalItemReward.LastOrDefault(i => i.Row != 0);
        if (item != null)
        {
            Callback.Fire((AtkUnitBase*)args.Addon, true, 0, item.Row);
        }
        else
        {
            Callback.Fire((AtkUnitBase*)args.Addon, true, 0);
        }
        IsEnable = false;
    }

    private static unsafe void OnAddonRequest(AddonEvent type, AddonArgs args)
    {
        Callback.Fire((AtkUnitBase*)args.Addon, true, 0);
    }
}
