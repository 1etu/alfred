using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;
using Alfred.Core.Ledger;

namespace Alfred.App.Views;

public partial class PaymentsView : UserControl
{
    private string? _brandSlug;
    private string? _categoryId;
    private DateOnly? _pickedDate;

    private PaymentsViewModel? _bound;

    public PaymentsView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;

            _bound = DataContext as PaymentsViewModel;

            _bound?.PrimaryRequested += OnPrimaryRequested;
        };

        ComposeTitle.Source = new BrandSource();
        ComposeTitle.Committed += (_, suggestion) => _brandSlug = (suggestion.Value as Brand)?.Slug;
        ComposeCategory.Source = new CategorySource();
        ComposeCategory.Committed += (_, suggestion) => _categoryId = (suggestion.Value as Category)?.Id;
        ComposeDate.Source = new DateSource();
        ComposeDate.Committed += (_, suggestion) => _pickedDate = suggestion.Value as DateOnly?;
    }

    private void OnPrimaryRequested(object? sender, EventArgs e)
    {
        Composer.Visibility = Visibility.Visible;
        ComposeTitle.FocusInput();
    }

    private void OnToggleComposer(object sender, RoutedEventArgs e)
    {
        Composer.Visibility = Composer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        if (Composer.Visibility == Visibility.Visible)
        {
            ComposeTitle.FocusInput();
        }
    }

    private void OnCommit(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PaymentsViewModel payments || string.IsNullOrWhiteSpace(ComposeTitle.Text))
        {
            return;
        }

        if (!decimal.TryParse(ComposeAmount.Text.Replace("₺", string.Empty).Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out decimal amount))
        {
            ComposeAmount.Focus();
            return;
        }

        EntryKind kind = KindPayment.IsChecked == true
            ? EntryKind.Payment
            : KindIncome.IsChecked == true ? EntryKind.Income : EntryKind.Expense;

        Cadence cadence = CadenceWeekly.IsChecked == true
            ? Cadence.Weekly
            : CadenceMonthly.IsChecked == true
                ? Cadence.Monthly
                : CadenceYearly.IsChecked == true ? Cadence.Yearly : Cadence.None;

        payments.Add(
            ComposeTitle.Text.Trim(),
            amount,
            kind,
            cadence,
            _pickedDate ?? DateOnly.FromDateTime(DateTime.Now),
            _brandSlug,
            _categoryId);

        ComposeTitle.Text = string.Empty;
        ComposeAmount.Text = string.Empty;
        ComposeCategory.Text = string.Empty;
        ComposeDate.Text = string.Empty;
        _brandSlug = null;
        _categoryId = null;
        _pickedDate = null;
        ComposeTitle.FocusInput();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PaymentsViewModel.EntryRow row })
        {
            row.Remove();
        }
    }
}
