using Microsoft.Win32;

namespace Alfred.Theme.Catalog;

public static class SystemTheme
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeName = "AppsUseLightTheme";

    public static bool IsDark()
    {
        using RegistryKey? personalize = Registry.CurrentUser.OpenSubKey(PersonalizePath);
        return personalize?.GetValue(AppsUseLightThemeName) is int value && value == 0;
    }
}
