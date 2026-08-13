using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Controls;

public partial class Sidebar : UserControl
{
    public Sidebar()
    {
        InitializeComponent();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.IsSettingsOpen = true;
        }
    }
}
