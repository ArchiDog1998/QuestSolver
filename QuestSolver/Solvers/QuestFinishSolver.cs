using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
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
        Plugin.EnableSolver<TalkSolver>();
        Plugin.EnableSolver<YesOrNoSolver>();

        Svc.Framework.Update += FrameworkUpdate;
    }

    private unsafe QuestItem? FindQuest()
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

        MovedLevels.Clear();

        return result;
    }

    protected override void Disable()
    {
        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;
        _quest = null;
    }

    private void FrameworkUpdate(IFramework framework)
    {
        CheckItemDetail();
        ClickResult();

        if (!Available) return;

        _quest = FindQuest();

        if (_quest == null)
        {
            IsEnable = false;
            return;
        }

        foreach (var level in _quest.Levels)
        {
            if (MovedLevels.Contains(level.RowId)) continue;
            if (CalculateOneLevel(level))
            {
                MovedLevels.Add(level.RowId);
                _validTargets.Clear();
                Svc.Log.Info("Finished Level " + level.RowId);
            }
            break;
        }
    }

    private readonly HashSet<IGameObject> _validTargets = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True for next.</returns>
    private bool CalculateOneLevel(Level level)
    {
        if (level.IsInSide())
        {
            FindValidTargets(level);

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

    private void FindValidTargets(Level level)
    {
        var eobjs = Svc.Data.GetExcelSheet<EObjName>();
        var name = eobjs?.GetRow(2000009)?.Singular.RawString;

        foreach (var item in Svc.Objects)
        {
            if (!level.IsInSide(item)) continue;
            if (!item.IsTargetable) continue;

            unsafe
            {
                var icon = item.Struct()->NamePlateIconId;

                if (icon is 71203 or 71205 //MSQ
                    )
                {
                    _validTargets.Add(item);
                }
                else if (name != null &&
                    eobjs?.GetRow(item.DataId)?.Singular.RawString == name)
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
    }

    private unsafe void ClickResult()
    {
        var result = (AtkUnitBase*)Svc.GameGui.GetAddonByName("JournalResult");
        if (result == null || !result->IsVisible) return;

        if (CallbackHelper.Fire(result, true, 0))
        {
            IsEnable = false;
        }
    }

    private unsafe void CheckItemDetail()
    {
        var request = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Request");
        if (request == null || !request->IsVisible) return;

        CallbackHelper.Fire(request, true, 0);
    }
}
