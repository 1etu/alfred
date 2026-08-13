using System.Globalization;
using Alfred.Core.Ledger;

namespace Alfred.App;

public static class MoneyFormat
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Compact(Money money)
    {
        string amount = money.Amount == decimal.Truncate(money.Amount)
            ? money.Amount.ToString("N0", Turkish)
            : money.Amount.ToString("N2", Turkish);

        return money.Currency == "TRY" ? "₺" + amount : amount + " " + money.Currency;
    }

    public static string WithSign(Money money, CashFlow flow) =>
        (flow == CashFlow.In ? "+" : string.Empty) + Compact(money);
}
