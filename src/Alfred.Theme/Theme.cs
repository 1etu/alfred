using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Alfred.Theme;

public sealed record Theme
{
    public required string Name { get; init; }

    public required ThemeVariant Variant { get; init; }

    public required IReadOnlyDictionary<string, Color> Colors { get; init; }

    public IReadOnlyDictionary<string, Color> IconColors { get; init; } = ReadOnlyDictionary<string, Color>.Empty;

    public bool IsDark => Variant == ThemeVariant.Dark;

    public Theme Extend(
        string name,
        IReadOnlyDictionary<string, Color>? colors = null,
        IReadOnlyDictionary<string, Color>? iconColors = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return this with
        {
            Name = name,
            Colors = Merge(Colors, colors),
            IconColors = Merge(IconColors, iconColors),
        };
    }

    private static IReadOnlyDictionary<string, Color> Merge(
        IReadOnlyDictionary<string, Color> baseline,
        IReadOnlyDictionary<string, Color>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return baseline;
        }

        Dictionary<string, Color> merged = new(baseline);

        foreach ((string key, Color color) in overrides)
        {
            merged[key] = color;
        }

        return new ReadOnlyDictionary<string, Color>(merged);
    }
}
