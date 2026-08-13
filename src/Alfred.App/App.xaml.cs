using System.Windows;
using Alfred.App.Input;
using Alfred.App.Preferences;
using Alfred.App.Theming;
using Alfred.App.ViewModels;
using Alfred.App.Views;

namespace Alfred.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        UserPreferences preferences = PreferencesStore.Load();
        Theme.Apply(Enum.TryParse(preferences.Theme, out ThemeVariant variant) ? variant : ThemeVariant.System);

        ShellWindow shell = new()
        {
            DataContext = new ShellViewModel(preferences, new ShortcutRegistry(preferences)),
        };

        shell.Show();
    }
}
