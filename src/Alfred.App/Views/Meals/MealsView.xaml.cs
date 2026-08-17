using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;
using Alfred.UIKit.Controls;

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
            input.Text.Trim() is { Length: > 0 } title)
        {
            slot.Add(title);
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
