using ImGuiNET;
using QuestSolver.Solvers;
using XIVConfigUI;
using XIVConfigUI.SearchableConfigs;

namespace QuestSolver.Windows;
public class SettingsWindow : ConfigWindow
{
    public override IEnumerable<Searchable> Searchables => Items.OfType<SolversItem>().SelectMany(i => i.Collections.SelectMany(i => i));
    protected override string Kofi => "B0B0IN5DX";
    protected override string Crowdin => "questsolver";

    protected override ConfigWindowItem[] GetItems()
    {
        var solvers = typeof(SettingsWindow).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.IsAssignableTo(typeof(BaseSolver)))
            .Select(i => (BaseSolver)Activator.CreateInstance(i)!);

        return [.. solvers.GroupBy(s => s.ItemType)
            .Select(i => new SolversItem(i.Key, [.. i]))];
    }

    public SettingsWindow() : base(typeof(SettingsWindow).Assembly.GetName())
    {
    }

    public override SearchableCollection Collection { get; } = new SearchableCollection(Plugin.Settings);

    protected override void DrawAbout()
    {
        base.DrawAbout();

        ImGui.Separator();

        Collection.DrawItems(0);
    }
}
