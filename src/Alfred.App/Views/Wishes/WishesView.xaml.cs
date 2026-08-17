using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;
using Alfred.UIKit.Suggest;

namespace Alfred.App.Views;

public partial class WishesView : UserControl
{
    private WishesViewModel? _bound;

    public WishesView()
    {
        InitializeComponent();
        Bar.TitleSource = new BrandSource();

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;
            _bound = DataContext as WishesViewModel;
            _bound?.PrimaryRequested += OnPrimaryRequested;
        };
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => Bar.FocusTitle();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not WishesViewModel wishes || Bar.Title.Length == 0)
        {
            return;
        }

        wishes.Add(Bar.Title, Bar.PickedAmount, Bar.PickedBrandSlug);
        Bar.Reset();
        Bar.FocusTitle();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WishesViewModel.WishRow row })
        {
            row.Remove();
        }
    }
}
