using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class RemindersView : UserControl
{
    private RemindersViewModel? _bound;

    public RemindersView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;
            _bound = DataContext as RemindersViewModel;
            _bound?.PrimaryRequested += OnPrimaryRequested;
        };
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => Bar.FocusTitle();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not RemindersViewModel reminders || Bar.Title.Length == 0)
        {
            return;
        }

        reminders.Add(Bar.Title, Bar.PickedDate ?? DateOnly.FromDateTime(DateTime.Now), Bar.PickedTime);
        Bar.Reset();
        Bar.FocusTitle();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RemindersViewModel.ReminderRow row })
        {
            row.Remove();
        }
    }
}
