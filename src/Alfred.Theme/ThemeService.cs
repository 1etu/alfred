using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Alfred.Theme.Defaults;

namespace Alfred.Theme;

public static class ThemeService
{
    private static ResourceDictionary? _applied;

    public static Theme Current { get; private set; } = DefaultThemes.Light;

    public static bool IsDark => Current.IsDark;

    public static event EventHandler<Theme>? Changed;

    public static void Apply(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        Current = theme;
        Swap(BuildBrushes(theme));
        Changed?.Invoke(null, theme);
    }

    private static void Swap(ResourceDictionary brushes)
    {
        if (Application.Current is null)
        {
            return;
        }

        Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;
        int index = _applied is null ? -1 : merged.IndexOf(_applied);

        if (index < 0)
        {
            merged.Insert(0, brushes);
        }
        else
        {
            merged[index] = brushes;
        }

        _applied = brushes;
    }

    private static ResourceDictionary BuildBrushes(Theme theme)
    {
        ResourceDictionary brushes = new();

        foreach ((string key, Color color) in theme.Colors)
        {
            SolidColorBrush brush = new(color);
            brush.Freeze();
            brushes[key] = brush;
        }

        return brushes;
    }
}
