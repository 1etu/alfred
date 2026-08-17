using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class TrashView : UserControl
{
    public TrashView()
    {
        InitializeComponent();
    }

    private void OnRestore(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TrashViewModel.TrashRow row })
        {
            row.Restore();
        }
    }

    private void OnForget(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TrashViewModel.TrashRow row })
        {
            row.Forget();
        }
    }
}
