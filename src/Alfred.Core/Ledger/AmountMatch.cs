namespace Alfred.Core.Ledger;

public sealed record AmountMatch(decimal Amount, string? CurrencyCode, int Start, int Length, string Cleaned);
