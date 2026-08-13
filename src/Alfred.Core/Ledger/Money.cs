using System.Globalization;

namespace Alfred.Core.Ledger;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Lira(decimal amount) => new(amount, "TRY");

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.##} {Currency}");
}
