using System.Globalization;
using System.Text.Json;

namespace Alfred.Core.Storage;

internal static class VaultText
{
    private const string DateFormat = "yyyy-MM-dd";

    private const string TimeFormat = "HH:mm:ss.fffffff";

    internal static string Encode(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

    internal static string Encode(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static string? EncodeOptional(DateOnly? date) => date?.ToString(DateFormat, CultureInfo.InvariantCulture);

    internal static string? EncodeOptional(TimeOnly? time) => time?.ToString(TimeFormat, CultureInfo.InvariantCulture);

    internal static string EncodeList<TItem>(List<TItem> items) => JsonSerializer.Serialize(items);

    internal static decimal DecodeAmount(string text) =>
        decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

    internal static DateOnly DecodeDate(string text) =>
        DateOnly.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);

    internal static DateOnly? DecodeOptionalDate(string? text) => text is null ? null : DecodeDate(text);

    internal static TimeOnly? DecodeOptionalTime(string? text) =>
        text is null ? null : TimeOnly.ParseExact(text, TimeFormat, CultureInfo.InvariantCulture);

    internal static List<TItem> DecodeList<TItem>(string text) =>
        JsonSerializer.Deserialize<List<TItem>>(text) ?? [];
}
