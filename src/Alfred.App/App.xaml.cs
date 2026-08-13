using System.Windows;
using Alfred.App.Theming;
using Alfred.App.ViewModels;
using Alfred.App.Views;

namespace Alfred.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Theme.Apply(ThemeVariant.System);

        ShellWindow shell = new()
        {
            DataContext = new ShellViewModel(),
        };

        shell.Show();
    }
}
