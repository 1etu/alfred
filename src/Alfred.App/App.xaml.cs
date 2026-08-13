using System.IO;
using System.Windows;
using System.Windows.Input;
using Alfred.App.Input;
using Alfred.App.Preferences;
using Alfred.App.Suggest;
using Alfred.App.Theming;
using Alfred.App.ViewModels;
using Alfred.App.Views;
using Alfred.Core.Storage;

namespace Alfred.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        UserPreferences preferences = PreferencesStore.Load();
        Theme.Apply(Enum.TryParse(preferences.Theme, out ThemeVariant variant) ? variant : ThemeVariant.System);

        Vault vault = new(Path.Combine(PreferencesStore.Folder, "alfred.json"));
        ShortcutRegistry registry = new(preferences);
        ShellViewModel model = new(preferences, registry, vault);

        registry.Register("shell.settings", "Open settings", "General",
            new KeyGesture(Key.OemComma, ModifierKeys.Control), () => model.IsSettingsOpen = true);
        registry.Register("shell.sidebar", "Toggle sidebar", "General",
            new KeyGesture(Key.OemBackslash, ModifierKeys.Control), () => model.IsSidebarExpanded = !model.IsSidebarExpanded);
        registry.Register("nav.today", "Go to Today", "Navigation",
            new KeyGesture(Key.D1, ModifierKeys.Control), () => model.Navigate(0));
        registry.Register("nav.upcoming", "Go to Upcoming", "Navigation",
            new KeyGesture(Key.D2, ModifierKeys.Control), () => model.Navigate(1));
        registry.Register("nav.todos", "Go to TODOs", "Navigation",
            new KeyGesture(Key.D3, ModifierKeys.Control), () => model.Navigate(3));
        registry.Register("nav.payments", "Go to Payments", "Navigation",
            new KeyGesture(Key.D4, ModifierKeys.Control), () => model.Navigate(7));

        _ = Task.Run(() => BrandCatalog.All.Count);

        ShellWindow shell = new()
        {
            DataContext = model,
        };

        shell.Show();
    }
}
