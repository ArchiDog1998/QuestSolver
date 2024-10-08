﻿namespace QuestSolver.Data;
internal struct BoolDelay(Func<float> getTime)
{
    private bool _lastValue = false;
    private DateTime _lastFoundTime = DateTime.Now;

    public bool Delay(bool value, float? timeOverride = null)
    {
        if (value && !_lastValue)
        {
            _lastFoundTime = DateTime.Now;
        }
        _lastValue = value;

        var span = DateTime.Now - _lastFoundTime;

        return span.TotalSeconds > (timeOverride ?? getTime()) && value;
    }
}
