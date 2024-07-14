using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace QuestSolver.Helpers;
internal class CallbackHelper
{
    private static DateTime _lastCall = DateTime.Now;
    public static unsafe bool Fire(AtkUnitBase* Base, bool updateState, params object[] values)
    {
        if (DateTime.Now - _lastCall < TimeSpan.FromSeconds(0.3)) return false;
        _lastCall = DateTime.Now;

        Callback.Fire(Base, updateState, values);
        return true;
    }
}
