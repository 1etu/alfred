using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;
using Alfred.Core.Ledger;
using Alfred.UIKit.Suggest;

namespace Alfred.App.Views;

public partial class PaymentsView : UserControl
{
    public PaymentsView()
    {
        InitializeComponent();
        Bar.TitleSource = new BrandSource();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is not PaymentsViewModel payments ||
            Bar.Title.Length == 0 ||
            Bar.PickedAmount is not decimal amount)
        {
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
            Bar.Title,
            amount,
            Bar.PickedCurrency,
            kind,
            cadence,
            Bar.PickedDate ?? DateOnly.FromDateTime(DateTime.Now),
            Bar.PickedBrandSlug);

        Bar.Reset();
        Bar.FocusTitle();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PaymentsViewModel.EntryRow row })
        {
            row.Remove();
        }
    }
}
