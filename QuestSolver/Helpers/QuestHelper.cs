using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace QuestSolver.Helpers;
internal static class QuestHelper
{
    public unsafe static bool CanAccept(this Quest quest)
    {
        if (QuestManager.Instance()->IsQuestAccepted(quest.RowId)) return true; //Accepted.
        if (QuestManager.IsQuestComplete(quest.RowId)) return false;//Finished
        if (!quest.PreviousFinished()) return false;//Previous Unfinisheds

        return true;
    }

    public static bool PreviousFinished(this Quest quest)
    {
        return quest.PreviousQuest.All(i => i.Row == 0 || QuestManager.IsQuestComplete(i.Row));
    }
}
