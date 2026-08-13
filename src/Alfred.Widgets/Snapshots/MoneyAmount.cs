using System.Globalization;

namespace Alfred.Widgets.Snapshots;

internal readonly record struct MoneyAmount(decimal Value, string CurrencyCode)
{
    public string ToRoundedText() => string.Create(CultureInfo.CurrentCulture, $"{Value:N0} {CurrencyCode}");

    public string ToExactText() => string.Create(CultureInfo.CurrentCulture, $"{Value:N2} {CurrencyCode}");
}
