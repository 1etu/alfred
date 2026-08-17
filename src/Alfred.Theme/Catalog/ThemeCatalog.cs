using Alfred.Theme.Defaults;

namespace Alfred.Theme.Catalog;

public static class ThemeCatalog
{
    public const string SystemSelection = "System";

    private static readonly Dictionary<string, Theme> Registered = new(StringComparer.OrdinalIgnoreCase)
    {
        [DefaultThemes.Light.Name] = DefaultThemes.Light,
        [DefaultThemes.Dark.Name] = DefaultThemes.Dark,
    };

    public static IReadOnlyCollection<Theme> All => Registered.Values;

    public static void Register(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Registered[theme.Name] = theme;
    }

    public static Theme? Find(string name) =>
        Registered.TryGetValue(name, out Theme? theme) ? theme : null;

    public static Theme Resolve(string? selection)
    {
        if (selection is null || string.Equals(selection, SystemSelection, StringComparison.OrdinalIgnoreCase))
        {
            return SystemDefault();
        }

        return Find(selection) ?? SystemDefault();
    }

    private static Theme SystemDefault() =>
        SystemTheme.IsDark() ? DefaultThemes.Dark : DefaultThemes.Light;
}
