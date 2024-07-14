using QuestSolver.Solvers;
using XIVConfigUI;
using XIVConfigUI.SearchableConfigs;

namespace QuestSolver.Windows;
public class SettingsWindow : ConfigWindow
{
    public override IEnumerable<Searchable> Searchables => Items.OfType<SolverItem>().SelectMany(i => i.Collection);
    protected override string Kofi => "B0B0IN5DX";

    protected override ConfigWindowItem[] GetItems()
    {
        var solverItems = typeof(SettingsWindow).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.IsAssignableTo(typeof(BaseSolver)))
            .Select(i => new SolverItem((BaseSolver)Activator.CreateInstance(i)!));

        return [.. solverItems];
    }

    public SettingsWindow() : base(typeof(SettingsWindow).Assembly.GetName())
    {
    }
}
