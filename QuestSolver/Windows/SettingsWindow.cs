using XIVConfigUI;

namespace QuestSolver.Windows;
public class SettingsWindow : ConfigWindow
{
    protected override ConfigWindowItem[] GetItems()
    {
        return [];
    }

    public SettingsWindow() : base(typeof(SettingsWindow).Assembly.GetName())
    {
    }
}
