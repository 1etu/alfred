using System.Windows;
using Microsoft.Win32;

namespace Alfred.App.Theming;

public enum ThemeVariant
{
    Light,
    Dark,
    System,
}

public static class Theme
{
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeName = "AppsUseLightTheme";
    private const int ThemeDictionaryIndex = 0;

    private static readonly Uri LightSource = new("Resources/Theme.Light.xaml", UriKind.Relative);
    private static readonly Uri DarkSource = new("Resources/Theme.Dark.xaml", UriKind.Relative);

    public static bool IsDark { get; private set; }

    public static void Apply(ThemeVariant variant)
    {
        bool useDark = variant switch
        {
            ThemeVariant.Dark => true,
            ThemeVariant.Light => false,
            _ => IsSystemDark(),
        };

        if (Application.Current is null)
        {
            return;
        }

        Application.Current.Resources.MergedDictionaries[ThemeDictionaryIndex] = new ResourceDictionary
        {
            Source = useDark ? DarkSource : LightSource,
        };

        IsDark = useDark;
    }

    private static bool IsSystemDark()
    {
        using RegistryKey? personalize = Registry.CurrentUser.OpenSubKey(PersonalizePath);
        return personalize?.GetValue(AppsUseLightThemeName) is int value && value == 0;
    }
}
