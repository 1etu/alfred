namespace Alfred.Core.Ledger;

public static class Currencies
{
    public static Currency Lira { get; } = new("TRY", "₺", "tr-TR", SymbolTrails: false);

    public static Currency Dollar { get; } = new("USD", "$", "en-US", SymbolTrails: false);

    public static Currency Euro { get; } = new("EUR", "€", "de-DE", SymbolTrails: true);

    public static Currency Pound { get; } = new("GBP", "£", "en-GB", SymbolTrails: false);

    public static IReadOnlyList<Currency> All { get; } = [Lira, Dollar, Euro, Pound];

    public static Currency? Find(string? code)
    {
        foreach (Currency currency in All)
        {
            if (string.Equals(currency.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return currency;
            }
        }

        return null;
    }
}
