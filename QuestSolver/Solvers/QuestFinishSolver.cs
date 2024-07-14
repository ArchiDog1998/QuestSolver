using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using System.ComponentModel;

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
    public byte Sequence => (byte)Array.IndexOf(Quest!.ToDoCompleteSeq, Work.Sequence);
    public MyQuest Quest { get; } = Svc.Data.GetExcelSheet<MyQuest>()?.GetRow((uint)QuestManager.Instance()->NormalQuests[index].QuestId | 0x10000)!;
    public Level[] Levels
    {
        get
        {
            var data = Svc.Data.GetExcelSheet<Level>();
            if (data == null) return [];

            var result = new List<Level>();

            for (int i = 0; i < 8; i++)
            {
                var id = Quest.ToDoLocation[Sequence, i];
                if (id == 0) continue;

                var item = data.GetRow(id);
                if (item == null) continue;

                result.Add(item);
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
    public override void Enable()
    {
        _quest ??= FindQuest();
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

        Svc.Log.Info("Do Quest: " + result.Quest.Name);
        Svc.Log.Error("Queue: " + result.Work.Sequence + " -> " + result.Sequence);

        return result;
    }

    public override void Disable()
    {
        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;
        _quest = null;
    }

    private void FrameworkUpdate(IFramework framework)
    {
        ClickResult();

        if (!Available) return;

        if (_quest == null
            || _quest.Levels == null)
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
                Svc.Log.Info("Finished Level " + level.RowId);
            }
            break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True for next.</returns>
    private bool CalculateOneLevel(Level level)
    {
        if (level.IsInSide())
        {
            var objects = FindValidTargets(level);

            if (objects == null || objects.Count == 0) return true;

            var obj = objects[0];
            if (!MoveHelper.MoveTo(obj.Position, 0))
            {
                TargetHelper.Interact(obj);
            }
        }
        else
        {
            MoveHelper.MoveTo(level);
        }
        return false;
    }

    private List<IGameObject> FindValidTargets(Level level)
    {
        var validTargets = new List<IGameObject>();
        foreach (var item in Svc.Objects)
        {
            if (!level.IsInSide(item)) continue;
            if (!item.IsTargetable) continue;

            unsafe
            {
                if (item.Struct()->EventState is 7)
                {
                    validTargets.Add(item);
                }
            }
        }
        return validTargets;
    }

    private unsafe void ClickResult()
    {
        var result = (AtkUnitBase*)Svc.GameGui.GetAddonByName("JournalResult");
        if (result == null || !result->IsVisible) return;

        Callback.Fire(result, true, 0);
    }
}
