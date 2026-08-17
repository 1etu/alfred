using System.Globalization;
using Alfred.Core.Ledger;

namespace Alfred.App;

public static class MoneyFormat
{
    public static string Compact(Money money)
    {
        if (Currencies.Find(money.Currency) is not Currency currency)
        {
            return Digits(money, CultureInfo.CurrentCulture) + " " + money.Currency;
        }

        string amount = Digits(money, CultureInfo.GetCultureInfo(currency.FormatCulture));

        return currency.SymbolTrails
            ? amount + " " + currency.Symbol
            : currency.Symbol + amount;
    }

    public static string WithSign(Money money, CashFlow flow) =>
        (flow == CashFlow.In ? "+" : string.Empty) + Compact(money);

    private static string Digits(Money money, CultureInfo culture) =>
        money.Amount == decimal.Truncate(money.Amount)
            ? money.Amount.ToString("N0", culture)
            : money.Amount.ToString("N2", culture);
}
