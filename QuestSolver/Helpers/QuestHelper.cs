using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace QuestSolver.Helpers;

internal enum QuestIconType : byte
{
    SideStoryQuest = 1,
    LoreQuest = 2,
    MainScenarioQuest = 3,
    SeasonQuest = 4,
    LeveQuest = 5,
    FunctionQuest = 8,
    FunctionQuest2 = 10,
    SideStoryQuest2 = 33,
    LoreQuest2 = 34,
}

internal class QuestWithTodo : Quest
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
    public QuestWithTodo Quest { get; } = Svc.Data.GetExcelSheet<QuestWithTodo>()?.GetRow((uint)QuestManager.Instance()->NormalQuests[index].QuestId | 0x10000)!;
    public unsafe Level[] Levels
    {
        get
        {
            var data = Svc.Data.GetExcelSheet<Level>();
            if (data == null) return [];
            var result = new List<Level>();

            //var questEvent = (QuestEventHandler*)EventFramework.Instance()->GetEventHandlerById(Quest.RowId);

            foreach (var sequence in Sequences)
            {
                for (byte i = 0; i < 8; i++)
                {
                    var id = Quest.ToDoLocation[sequence, i];

                    //if (questEvent->IsTodoChecked(Player.BattleChara, i))
                    //{
                    //    Svc.Log.Info("Finished Todo");
                    //    continue;
                    //}

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

internal static class QuestHelper
{
    public unsafe static bool CanAccept(this Quest quest)
    {
        if (QuestManager.Instance()->IsQuestAccepted(quest.RowId)) return true; //Accepted.
        if (QuestManager.IsQuestComplete(quest.RowId)) return false;//Finished
        if (!quest.PreviousFinished()) return false;

        return true;
    }

    public static bool PreviousFinished(this Quest quest)
    {
        return quest.PreviousQuest.All(i => i.Row == 0 || QuestManager.IsQuestComplete(i.Row));
    }

    public static QuestIconType GetQuestIcon(this QuestItem quest)
    {
        return (QuestIconType)(byte)quest.Quest.EventIconType.Row;
    }

    public unsafe static QuestItem? FindBestQuest(uint questId = 0)
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

        var result = quests.FirstOrDefault(q => q.Quest.RowId == questId);

        if (result != null) return result;

        var priorityQuests = quests.Where(q => q.GetQuestIcon() is QuestIconType.MainScenarioQuest);

        if (!priorityQuests.Any())
        {
            priorityQuests = quests.Where(q => q.GetQuestIcon() is QuestIconType.FunctionQuest or QuestIconType.FunctionQuest2);
        }

        if (!priorityQuests.Any())
        {
            priorityQuests = quests;
        }

        return priorityQuests.FirstOrDefault(q => !(q.Quest?.IsRepeatable ?? true))
            ?? priorityQuests.FirstOrDefault();
    }

    public static bool CanTake(this Leve leve)
    {
        if (!leve.CanTake<CraftLeve>(CanTake)) return false;
        if (!leve.CanTake<CompanyLeve>(CanTake)) return false;
        if (!leve.CanTake<GatheringLeve>(CanTake)) return false;
        if (!leve.CanTake<BattleLeve>(CanTake)) return false;

        return true;
    }

    private static bool CanTake<T>(this Leve leve, Func<T, bool> predict) where T : ExcelRow
    {
        if (Svc.Data.GetExcelSheet<T>()?.GetRow((uint)leve.DataId) is T craftLeve)
        {
            if (!predict(craftLeve)) return false;
        }
        return true;
    }

    private static bool CanTake(CraftLeve craftLeve)
    {
        var count = craftLeve.Repeats + 1;

        foreach (var item in craftLeve.UnkData3)
        {
            if (item.ItemCount == 0 || item.Item == 0) continue;

            var itemCount = InventoryHelper.ItemCount((uint)item.Item, true) / item.ItemCount
                          + InventoryHelper.ItemCount((uint)item.Item, false) / item.ItemCount;

            if (itemCount < count) return false;
        }
        return true;
    }

    private static bool CanTake(CompanyLeve companyLeve)
    {
        return true;
    }

    private static bool CanTake(GatheringLeve gatheringLeve)
    {
        return true;
    }

    private static bool CanTake(BattleLeve battleLeve)
    {
        return true;
    }

    public static Level? GetLeveStartLevel(this Leve leve)
    {
        if (leve.LevelStart.Row != 0)
        {
            return leve.LevelLevemete.Value;
        }
        else
        {
            var otherLeve = Svc.Data.GetExcelSheet<Leve>()?.FirstOrDefault(l => l.ClassJobLevel == leve.ClassJobLevel
                && l.LevelStart.Row != 0);
            return otherLeve?.LevelLevemete.Value;
        }
    }
}
