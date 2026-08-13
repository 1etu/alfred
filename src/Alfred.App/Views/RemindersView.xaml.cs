using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class RemindersView : UserControl
{
    private DateOnly? _pickedDate;

    private RemindersViewModel? _bound;

    public RemindersView()
    {
        InitializeComponent();
        QuickDate.Source = new DateSource();
        QuickDate.Committed += (_, suggestion) => _pickedDate = suggestion.Value as DateOnly?;

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;

            _bound = DataContext as RemindersViewModel;

            _bound?.PrimaryRequested += OnPrimaryRequested;
        };
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => QuickTitle.FocusInput();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not RemindersViewModel reminders || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        TimeOnly? at = null;
        if (TimeOnly.TryParseExact(QuickTime.Text.Trim(), ["H:mm", "HH:mm", "H.mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsed))
        {
            at = parsed;
        }

        reminders.Add(QuickTitle.Text.Trim(), _pickedDate ?? DateOnly.FromDateTime(DateTime.Now), at);
        QuickTitle.Text = string.Empty;
        QuickDate.Text = string.Empty;
        QuickTime.Text = string.Empty;
        _pickedDate = null;
        QuickTitle.FocusInput();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RemindersViewModel.ReminderRow row })
        {
            row.Remove();
        }
    }
}
