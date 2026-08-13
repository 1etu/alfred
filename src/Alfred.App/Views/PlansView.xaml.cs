using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Alfred.App.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class PlansView : UserControl
{
    private DateOnly? _pickedDate;

    public PlansView()
    {
        InitializeComponent();
        QuickDate.Source = new DateSource();
        QuickDate.Committed += (_, suggestion) => _pickedDate = suggestion.Value as DateOnly?;
    }

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not PlansViewModel plans || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        plans.Add(QuickTitle.Text.Trim(), _pickedDate);
        QuickTitle.Text = string.Empty;
        QuickDate.Text = string.Empty;
        _pickedDate = null;
        QuickTitle.FocusInput();
    }

    private void OnRowClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlansViewModel.PlanRow row })
        {
            row.IsExpanded = !row.IsExpanded;
        }
    }

    private void OnAddStep(object sender, EventArgs e)
    {
        if (sender is SmartInput input &&
            input.DataContext is PlansViewModel.PlanRow row &&
            !string.IsNullOrWhiteSpace(input.Text))
        {
            row.AddStep(input.Text.Trim());
            input.Text = string.Empty;
            input.FocusInput();
        }
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlansViewModel.PlanRow row })
        {
            row.Remove();
        }

        e.Handled = true;
    }
}
