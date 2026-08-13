using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class WishesView : UserControl
{
    private string? _brandSlug;

    private WishesViewModel? _bound;

    public WishesView()
    {
        InitializeComponent();
        QuickTitle.Source = new BrandSource();
        QuickTitle.Committed += (_, suggestion) => _brandSlug = (suggestion.Value as Brand)?.Slug;

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;

            _bound = DataContext as WishesViewModel;

            _bound?.PrimaryRequested += OnPrimaryRequested;
        };
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => QuickTitle.FocusInput();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not WishesViewModel wishes || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        decimal? price = null;
        if (decimal.TryParse(QuickPrice.Text.Replace("₺", string.Empty).Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out decimal parsed))
        {
            price = parsed;
        }

        wishes.Add(QuickTitle.Text.Trim(), price, _brandSlug);
        QuickTitle.Text = string.Empty;
        QuickPrice.Text = string.Empty;
        _brandSlug = null;
        QuickTitle.FocusInput();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WishesViewModel.WishRow row })
        {
            row.Remove();
        }
    }
}
