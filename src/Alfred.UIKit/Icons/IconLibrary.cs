using System.Windows;
using System.Windows.Media;
using Alfred.Theme;

namespace Alfred.UIKit.Icons;

public static class IconLibrary
{
    private static readonly Dictionary<string, DrawingImage> Authored = [];
    private static bool _isTracking;

    public static ImageSource Resolve(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return Application.Current.Resources[key] as ImageSource
            ?? throw new InvalidOperationException($"Icon '{key}' is not registered.");
    }

    public static void Track(ResourceDictionary root)
    {
        ArgumentNullException.ThrowIfNull(root);

        Collect(root);

        if (_isTracking)
        {
            Refresh(ThemeService.Current);
            return;
        }

        _isTracking = true;
        ThemeService.Changed += (_, theme) => Refresh(theme);
        Refresh(ThemeService.Current);
    }

    private static void Collect(ResourceDictionary dictionary)
    {
        foreach (object key in dictionary.Keys)
        {
            if (key is string name && dictionary[key] is DrawingImage image)
            {
                Authored[name] = image;
            }
        }

        foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
        {
            Collect(merged);
        }
    }

    private static void Refresh(Alfred.Theme.Theme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        ResourceDictionary resources = Application.Current.Resources;

        foreach ((string key, DrawingImage authored) in Authored)
        {
            if (theme.IconColors.TryGetValue(key, out Color color))
            {
                resources[key] = Tint(authored, color);
            }
            else if (resources.Contains(key))
            {
                resources.Remove(key);
            }
        }
    }

    private static DrawingImage Tint(DrawingImage authored, Color color)
    {
        DrawingImage tinted = authored.Clone();

        if (FindPrimaryColor(tinted.Drawing) is Color primary)
        {
            SolidColorBrush replacement = new(color);
            replacement.Freeze();
            ReplaceColor(tinted.Drawing, primary, replacement);
        }

        tinted.Freeze();
        return tinted;
    }

    private static Color? FindPrimaryColor(Drawing drawing)
    {
        switch (drawing)
        {
            case DrawingGroup group:
                foreach (Drawing child in group.Children)
                {
                    if (FindPrimaryColor(child) is Color found)
                    {
                        return found;
                    }
                }

                return null;

            case GeometryDrawing { Brush: SolidColorBrush brush } when brush.Color.A > 0:
                return brush.Color;

            case GeometryDrawing { Pen.Brush: SolidColorBrush penBrush } when penBrush.Color.A > 0:
                return penBrush.Color;

            default:
                return null;
        }
    }

    private static void ReplaceColor(Drawing drawing, Color primary, SolidColorBrush replacement)
    {
        switch (drawing)
        {
            case DrawingGroup group:
                foreach (Drawing child in group.Children)
                {
                    ReplaceColor(child, primary, replacement);
                }

                break;

            case GeometryDrawing geometry:
                if (geometry.Brush is SolidColorBrush brush && brush.Color == primary)
                {
                    geometry.Brush = replacement;
                }

                if (geometry.Pen?.Brush is SolidColorBrush penBrush && penBrush.Color == primary)
                {
                    geometry.Pen.Brush = replacement;
                }

                break;
        }
    }
}
