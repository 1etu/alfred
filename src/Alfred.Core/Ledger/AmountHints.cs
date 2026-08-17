using System.Globalization;
using System.Text.RegularExpressions;

namespace Alfred.Core.Ledger;

public static partial class AmountHints
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    [GeneratedRegex(
        @"(?<![\p{L}\d:,.])(?<pre>[₺$€£])?\s?(?<value>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d{1,12}(?:[.,]\d{1,4})?)\s?(?<scale>[km])?\s?(?<suffix>₺|\$|€|£|tl|try|usd|eur|gbp)?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex TrailingAmount();

    public static AmountMatch? Match(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        Match match = TrailingAmount().Match(text);

        if (!match.Success || !TryRead(match, out decimal amount))
        {
            return null;
        }

        string cleaned = text.Remove(match.Index, match.Length).Trim();

        return new AmountMatch(
            amount,
            ReadCurrency(match),
            match.Index,
            match.Length,
            cleaned);
    }

    public static bool TryExtract(string text, out string cleaned, out decimal amount)
    {
        cleaned = text;
        amount = 0;

        if (Match(text) is not AmountMatch match)
        {
            return false;
        }

        cleaned = match.Cleaned;
        amount = match.Amount;
        return true;
    }

    private static string? ReadCurrency(Match match)
    {
        string marker = match.Groups["pre"].Value;

        if (marker.Length == 0)
        {
            marker = match.Groups["suffix"].Value;
        }

        return marker.ToLowerInvariant() switch
        {
            "₺" or "tl" or "try" => Currencies.Lira.Code,
            "$" or "usd" => Currencies.Dollar.Code,
            "€" or "eur" => Currencies.Euro.Code,
            "£" or "gbp" => Currencies.Pound.Code,
            _ => null,
        };
    }

    private static bool TryRead(Match match, out decimal amount)
    {
        amount = 0;
        string digits = match.Groups["value"].Value;
        string scale = match.Groups["scale"].Value;

        if (scale.Length == 0)
        {
            return decimal.TryParse(digits, NumberStyles.Number, Turkish, out amount);
        }

        if (digits.Contains('.') && digits.Contains(','))
        {
            return false;
        }

        if (!decimal.TryParse(digits.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal mantissa))
        {
            return false;
        }

        amount = mantissa * (char.ToLowerInvariant(scale[0]) == 'k' ? 1_000 : 1_000_000);
        return true;
    }
}
