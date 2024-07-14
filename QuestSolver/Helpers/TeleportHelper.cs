using ECommons.DalamudServices;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace QuestSolver.Helpers;
internal class TeleportHelper
{
    public unsafe static IEnumerable<Aetheryte> ValidAetherytes
    {
        get
        {
            IEnumerable<Aetheryte> aetherytes = Svc.Data.GetExcelSheet<Aetheryte>()?.Where(i => i.Level[0].Row != 0) ?? [];

            var teleport = Telepo.Instance();
            if (teleport == null) return [];

            teleport->UpdateAetheryteList();

            var validIds = teleport->TeleportList.Select(i => i.AetheryteId);

            return aetherytes.Where(i => validIds.Contains(i.RowId));
        }
    }

    public static bool Teleport(Vector3 destination, uint territoryId)
    {
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Casting]) return false;
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return false;
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51]) return false;

        var aetheries = ValidAetherytes.Where(a => a.Territory.Row == territoryId);

        var minAethery = aetheries.MinBy(i =>
        {
            var level = i.Level[0].Value;
            if (level == null) return float.MaxValue;
            return Vector2.DistanceSquared(level.ToLocation().ToVector2(), destination.ToVector2());
        });

        if (minAethery == null) return false;
        //MapUtil.WorldToMap()
        return Teleport(minAethery);
    }

    private static DateTime _lastCall = DateTime.Now;

    private static unsafe bool Teleport(Aetheryte aetheryte)
    {
        if (DateTime.Now - _lastCall < TimeSpan.FromSeconds(0.3)) return false;
        _lastCall = DateTime.Now;

        if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 5) != 0)
            return false;

        return Telepo.Instance()->Teleport(aetheryte.RowId, (byte)aetheryte.SubRowId);
    }
}
