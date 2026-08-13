using System.Windows;
using System.Windows.Controls;
using Alfred.App.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class MealsView : UserControl
{
    public MealsView()
    {
        InitializeComponent();
    }

    private void OnAddMeal(object sender, EventArgs e)
    {
        if (sender is SmartInput input &&
            input.DataContext is MealsViewModel.SlotModel slot &&
            !string.IsNullOrWhiteSpace(input.Text))
        {
            slot.Add(input.Text.Trim());
            input.Text = string.Empty;
        }
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MealsViewModel.MealRow row })
        {
            row.Remove();
        }
    }
}
