using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using Alfred.Theme.Defaults;

namespace Alfred.Theme.Storage;

public static class ThemeJson
{
    private sealed record ThemeFile(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("base")] string? Base,
        [property: JsonPropertyName("colors")] SortedDictionary<string, string>? Colors,
        [property: JsonPropertyName("icons")] SortedDictionary<string, string>? Icons);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Theme? Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        ThemeFile? file;

        try
        {
            file = JsonSerializer.Deserialize<ThemeFile>(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (file?.Name is not { Length: > 0 } name)
        {
            return null;
        }

        Theme baseline = string.Equals(file.Base, DefaultThemes.Dark.Name, StringComparison.OrdinalIgnoreCase)
            ? DefaultThemes.Dark
            : DefaultThemes.Light;

        if (!TryParseAll(file.Colors, out Dictionary<string, Color>? colors) ||
            !TryParseAll(file.Icons, out Dictionary<string, Color>? icons))
        {
            return null;
        }

        return baseline.Extend(name, colors, icons);
    }

    public static string Write(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        Theme baseline = theme.IsDark ? DefaultThemes.Dark : DefaultThemes.Light;
        SortedDictionary<string, string> colors = [];

        foreach ((string key, Color color) in theme.Colors)
        {
            if (!baseline.Colors.TryGetValue(key, out Color inherited) || inherited != color)
            {
                colors[key] = FormatColor(color);
            }
        }

        SortedDictionary<string, string> icons = [];

        foreach ((string key, Color color) in theme.IconColors)
        {
            icons[key] = FormatColor(color);
        }

        ThemeFile file = new(
            theme.Name,
            baseline.Name,
            colors.Count == 0 ? null : colors,
            icons.Count == 0 ? null : icons);

        return JsonSerializer.Serialize(file, SerializerOptions);
    }

    private static bool TryParseAll(
        IReadOnlyDictionary<string, string>? entries,
        out Dictionary<string, Color>? colors)
    {
        colors = null;

        if (entries is null)
        {
            return true;
        }

        Dictionary<string, Color> parsed = [];

        foreach ((string key, string text) in entries)
        {
            if (!TryParseColor(text, out Color color))
            {
                return false;
            }

            parsed[key] = color;
        }

        colors = parsed;
        return true;
    }

    private static bool TryParseColor(string text, out Color color)
    {
        color = default;

        if (text.Length is not (7 or 9) || text[0] != '#')
        {
            return false;
        }

        string hex = text[1..];

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            return false;
        }

        color = hex.Length == 6
            ? Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);

        return true;
    }

    private static string FormatColor(Color color) => color.A == byte.MaxValue
        ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
}
