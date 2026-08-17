using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Alfred.App.ViewModels;
using Alfred.UIKit.Controls;

namespace Alfred.App.Views;

public partial class PlansView : UserControl
{
    public PlansView()
    {
        InitializeComponent();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is PlansViewModel plans && Bar.Title.Length > 0)
        {
            plans.Add(Bar.Title, Bar.PickedDate);
            Bar.Reset();
            Bar.FocusTitle();
        }
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
