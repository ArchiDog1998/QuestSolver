using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Application.Network.WorkDefinitions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
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

            var questEvent = (QuestEventHandler*)EventFramework.Instance()->GetEventHandlerById(Quest.RowId);

            foreach (var sequence in Sequences)
            {
                for (byte i = 0; i < 8; i++)
                {
                    var id = Quest.ToDoLocation[sequence, i];

                    if (questEvent->IsTodoChecked(Player.BattleChara, i))
                    {
                        Svc.Log.Info("Finished Todo");
                        continue;
                    }

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

    public unsafe static QuestItem? FindBestQuest()
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

        quests[0].Quest.EventIconType
        return quests.FirstOrDefault(q => !(q.Quest?.IsRepeatable ?? true))
            ?? quests.FirstOrDefault();
    }
}
