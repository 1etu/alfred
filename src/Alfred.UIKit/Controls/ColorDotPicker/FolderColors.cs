using Alfred.Localization;
using Alfred.Theme;

namespace Alfred.UIKit.Controls;

public static class FolderColors
{
    public static IReadOnlyList<FolderColor> All { get; } =
    [
        new(ThemeKeys.FolderRed, LocalizationKeys.ColorRed),
        new(ThemeKeys.FolderOrange, LocalizationKeys.ColorOrange),
        new(ThemeKeys.FolderYellow, LocalizationKeys.ColorYellow),
        new(ThemeKeys.FolderGreen, LocalizationKeys.ColorGreen),
        new(ThemeKeys.FolderTeal, LocalizationKeys.ColorTeal),
        new(ThemeKeys.FolderBlue, LocalizationKeys.ColorBlue),
        new(ThemeKeys.FolderPurple, LocalizationKeys.ColorPurple),
        new(ThemeKeys.FolderPink, LocalizationKeys.ColorPink),
    ];

    public static FolderColor Resolve(string? brushKey)
    {
        foreach (FolderColor color in All)
        {
            if (color.BrushKey == brushKey)
            {
                return color;
            }
        }

        return All[0];
    }
}
