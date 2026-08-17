using System.Globalization;
using System.Text.RegularExpressions;

namespace Alfred.Core.Ledger;

public static partial class AmountHints
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    [GeneratedRegex(
        @"(?<![\p{L}\d:,.])₺?\s?(?<value>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d{1,12}(?:[.,]\d{1,4})?)\s?(?<scale>[km])?\s?(?:₺|tl)?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex TrailingAmount();

    public static bool TryExtract(string text, out string cleaned, out decimal amount)
    {
        cleaned = text;
        amount = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        Match match = TrailingAmount().Match(text);

        if (!match.Success || !TryRead(match, out amount))
        {
            return false;
        }

        cleaned = text.Remove(match.Index, match.Length).Trim();
        return true;
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
