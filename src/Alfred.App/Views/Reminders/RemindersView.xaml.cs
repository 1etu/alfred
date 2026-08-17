using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class RemindersView : UserControl
{
    public RemindersView()
    {
        InitializeComponent();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is RemindersViewModel model && Bar.Title.Length > 0)
        {
            model.Add(Bar.Title, Bar.PickedDate ?? DateOnly.FromDateTime(DateTime.Now), Bar.PickedTime);
            Bar.Reset();
            Bar.FocusTitle();
        }
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RemindersViewModel.ReminderRow row })
        {
            row.Remove();
        }
    }
}
