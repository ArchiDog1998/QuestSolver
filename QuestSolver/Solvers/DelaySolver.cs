using QuestSolver.Data;
using XIVConfigUI.Attributes;

namespace QuestSolver.Solvers;
internal abstract class DelaySolver : BaseSolver
{
    protected BoolDelay _delay;

    [UI("Delay", Order = 1)]
    public float Delay { get; set; } = 3;

    protected DelaySolver()
    {
        _delay = new(() => Delay);
    }
}
