using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using System.ComponentModel;
using System.Numerics;

namespace QuestSolver.Solvers;

internal class MyQuest : Quest
{
    public uint[,] ToDoLocation { get; set; } = new uint[24, 8];
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);

        for (int i = 0; i < 24; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ToDoLocation[i, j] = parser.ReadColumn<uint>(1222 + i + j * 24);
            }
        }
    }
}

internal unsafe class QuestItem(int index)
{
    public QuestWork Work => QuestManager.Instance()->NormalQuests[index];
    public byte[] Sequences
    {
        get
        {
            var result = new List<byte>();
            var data = Quest!.ToDoCompleteSeq;
            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item == Work.Sequence)
                {
                    result.Add((byte)i);
                }
            }
            return [.. result];
        }
    }
    public MyQuest Quest { get; } = Svc.Data.GetExcelSheet<MyQuest>()?.GetRow((uint)QuestManager.Instance()->NormalQuests[index].QuestId | 0x10000)!;
    public Level[] Levels
    {
        get
        {
            var data = Svc.Data.GetExcelSheet<Level>();
            if (data == null) return [];

            var result = new List<Level>();

            foreach (var sequence in Sequences)
            {
                for (int i = 0; i < 8; i++)
                {
                    var id = Quest.ToDoLocation[sequence, i];
                    if (id == 0) continue;

                    var item = data.GetRow(id);
                    if (item == null) continue;

                    result.Add(item);
                }
            }

            return [.. result];
        }
    }
}


[Description("Quest Finisher")]
internal class QuestFinishSolver : BaseSolver
{
    public override uint Icon => 1;
    private readonly List<uint> MovedLevels = [];

    QuestItem? _quest = null;
    protected override void Enable()
    {
        Plugin.IsEnableSolver<TalkSolver>();
        Plugin.IsEnableSolver<YesOrNoSolver>();

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnAddonJournalResult);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "JournalResult", OnAddonJournalResult);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Request", OnAddonRequest);

        Svc.Framework.Update += FrameworkUpdate;
    }

    private unsafe void FindQuest()
    {
        List<QuestItem> quests = [];

        var normals = QuestManager.Instance()->NormalQuests;
        for (int i = 0; i < normals.Length; i++)
        {
            var item = normals[i];

            if (item.QuestId == 0) continue;
            if (item.IsHidden) continue;

            quests.Add(new QuestItem(i));
        }
        var result = quests.FirstOrDefault(q => !(q.Quest?.IsRepeatable ?? true))
            ?? quests.FirstOrDefault();

        if (result?.Quest.RowId == _quest?.Quest.RowId) return;

        Svc.Log.Info("Try to finish " +  result?.Quest.Name.RawString);
        MovedLevels.Clear();
        _quest = result;
    }

    protected override void Disable()
    {
        Svc.AddonLifecycle.UnregisterListener(OnAddonJournalResult);
        Svc.AddonLifecycle.UnregisterListener(OnAddonRequest);

        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;

        MovedLevels.Clear();
        _validTargets.Clear();

        _quest = null;
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (!Available) return;
        if (WaitForCombat()) return;

        FindQuest();

        if (_quest == null)
        {
            IsEnable = false;
            return;
        }

        foreach (var level in _quest.Levels)
        {
            if (MovedLevels.Contains(level.RowId)) continue;
            if (CalculateOneLevel(level, _quest.Quest))
            {
                MovedLevels.Add(level.RowId);
                _validTargets.Clear();
                Svc.Log.Info("Finished Level " + level.RowId);
            }
            break;
        }
    }

    private unsafe bool WaitForCombat()
    {
        if (!MountHelper.InCombat) return false;

        foreach (var obj in Svc.Objects)
        {
            if (obj is not IBattleChara) continue;
            if (obj.Struct()->EventId.ContentId is FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandlerType.Quest) return true;
        }

        return false;
    }

    private readonly HashSet<IGameObject> _validTargets = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True for next.</returns>
    private bool CalculateOneLevel(Level level, MyQuest quest)
    {
        if (level.IsInSide())
        {
            FindValidTargets(level, quest);

            if (_validTargets.Count == 0) return true;

            var obj = _validTargets.MinBy(t => Vector3.DistanceSquared(t.Position, Player.Object.Position))!;
            if (!MoveHelper.MoveTo(obj.Position, 0))
            {
                TargetHelper.Interact(obj);
                _validTargets.Remove(obj);
            }
        }
        else
        {
            MoveHelper.MoveTo(level);
        }
        return false;
    }

    private void FindValidTargets(Level level, MyQuest quest)
    {
        var eobjs = Svc.Data.GetExcelSheet<EObj>();

        foreach (var item in Svc.Objects)
        {
            if (!level.IsInSide(item)) continue;
            if (!item.IsTargetable) continue;

            unsafe
            {
                var icon = item.Struct()->NamePlateIconId;

                if (icon is 71203 or 71205 //MSQ
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

        var invalidItems = _validTargets.Where(i => !i.IsValid());
        foreach (var item in invalidItems)
        {
            _validTargets.Remove(item);
        }
    }

    private unsafe void OnAddonJournalResult(AddonEvent type, AddonArgs args)
    {
        var item = _quest?.Quest.OptionalItemReward.LastOrDefault(i => i.Row != 0);
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

    private unsafe void OnAddonRequest(AddonEvent type, AddonArgs args)
    {
        Callback.Fire((AtkUnitBase*)args.Addon, true, 0);
    }
}
