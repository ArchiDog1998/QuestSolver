using ECommons.GameHelpers;
using System.Numerics;

namespace QuestSolver.Helpers;
internal static class MoveHelper
{
    private static Vector3 lastPos = default;
    private static DateTime stopTime = DateTime.Now;
    public static bool MoveTo(Vector3 destination)
    {
        if (Plugin.Vnavmesh.IsRunning() && !Plugin.Vnavmesh.PathfindInProgress())
        {
            var dis = Vector3.DistanceSquared(Player.Object.Position, lastPos);
            if (dis < 0.001 && DateTime.Now - stopTime > TimeSpan.FromSeconds(3))
            {
                //Re calculate.
                Plugin.Vnavmesh.Stop();
                stopTime = DateTime.Now;
            }
            lastPos = Player.Object.Position;
            return true;
        }

        if (Vector3.DistanceSquared(Player.Object.Position, destination) > 1)
        {
            if (!MountHelper.IsMount)
            {
                MountHelper.TryMount();
            }
            else
            {
                if (!Plugin.Vnavmesh.PathfindInProgress())
                {
                    Plugin.Vnavmesh.PathfindAndMoveTo(destination, false);
                }
            }
            return true;
        }
        else if (MountHelper.IsMount)
        {
            MountHelper.TryDisMount();
        }

        return false;
    }
}
