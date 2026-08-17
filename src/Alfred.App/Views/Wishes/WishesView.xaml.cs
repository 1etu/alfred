using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class WishesView : UserControl
{
    public WishesView()
    {
        InitializeComponent();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is WishesViewModel model && Bar.Title.Length > 0)
        {
            model.Add(Bar.Title, Bar.PickedAmount, Bar.PickedBrandSlug, Bar.PickedCurrency);
            Bar.Reset();
            Bar.FocusTitle();
        }
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WishesViewModel.WishRow row })
        {
            row.Remove();
        }
    }
}
