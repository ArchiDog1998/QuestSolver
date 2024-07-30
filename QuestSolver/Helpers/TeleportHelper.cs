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

        var map = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRow(territoryId)?.Map.Value;

        var minAethery = aetheries.MinBy(i =>
        {
            Vector2 loc;

            var marker = Svc.Data.GetExcelSheet<MapMarker>()?.FirstOrDefault(m => m.DataType == 3 && m.DataKey == i.RowId);

            if (marker != null && map != null)
            {
                loc = MarkerToWorldPosition(map, new(marker.X, marker.Y));
            }
            else
            {
                var level = i.Level[0].Value;
                if (level == null) return float.MaxValue;
                loc = level.ToLocation().ToVector2();
            }

            return Vector2.DistanceSquared(loc, destination.ToVector2());
        });

        if (minAethery == null)
        {
            //TODO, hwo to deal with the one without aethery?
            Svc.Log.Error("Don't know how to teleport to " + territoryId);
            return false;
        }
        //MapUtil.WorldToMap()
        return Teleport(minAethery);
    }

    /// <summary>
    /// From https://github.com/una-xiv/umbra/blob/main/Umbra.Game/src/Zone/Marker/ZoneMarkerFactory.cs#L225-L233
    /// </summary>
    /// <param name="map"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    private static Vector2 MarkerToWorldPosition(Lumina.Excel.GeneratedSheets.Map map, Vector2 pos)
    {
        Vector2 v = default;

        v.X = ((pos.X - 1024f) / (map.SizeFactor / 100.0f)) - (map.OffsetX * (map.SizeFactor / 100.0f));
        v.Y = ((pos.Y - 1024f) / (map.SizeFactor / 100.0f)) - (map.OffsetY * (map.SizeFactor / 100.0f));

        return v;
    }

    private static DateTime _lastCall = DateTime.Now - TimeSpan.FromSeconds(6);

    private static unsafe bool Teleport(Aetheryte aetheryte)
    {
        if (DateTime.Now - _lastCall < TimeSpan.FromSeconds(6)) return false;
        _lastCall = DateTime.Now;

        if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 5) != 0)
            return false;

        return Telepo.Instance()->Teleport(aetheryte.RowId, (byte)aetheryte.SubRowId);
    }
}
