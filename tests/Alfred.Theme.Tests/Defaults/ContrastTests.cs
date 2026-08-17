using System.Windows.Media;
using Alfred.Theme;
using Alfred.Theme.Defaults;
using Xunit;

namespace Alfred.Theme.Tests.Defaults;

public class ContrastTests
{
    private static readonly (string Fore, string Back, double Minimum)[] Pairs =
    [
        (ThemeKeys.TextPrimary, ThemeKeys.SheetBackground, 4.5),
        (ThemeKeys.TextPrimary, ThemeKeys.ShellBackground, 4.5),
        (ThemeKeys.TextPrimary, ThemeKeys.CardBackground, 4.5),
        (ThemeKeys.TextSecondary, ThemeKeys.SheetBackground, 4.5),
        (ThemeKeys.TextSecondary, ThemeKeys.ShellBackground, 4.5),
        (ThemeKeys.TextSecondary, ThemeKeys.CardBackground, 4.5),
        (ThemeKeys.SidebarText, ThemeKeys.SidebarBackground, 4.5),
        (ThemeKeys.SidebarBadge, ThemeKeys.SidebarBackground, 4.5),
        (ThemeKeys.SidebarBadgeSelected, ThemeKeys.SidebarRowSelected, 4.5),
        (ThemeKeys.ChromeGlyph, ThemeKeys.TitleBarBackground, 3.0),
        (ThemeKeys.Accent, ThemeKeys.SheetBackground, 3.0),
    ];

    public static TheoryData<string> Themes => [DefaultThemes.Light.Name, DefaultThemes.Dark.Name];

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextMeetsWcagContrast(string themeName)
    {
        Theme theme = themeName == DefaultThemes.Dark.Name ? DefaultThemes.Dark : DefaultThemes.Light;

        foreach ((string fore, string back, double minimum) in Pairs)
        {
            double ratio = Ratio(theme.Colors[fore], theme.Colors[back]);

            Assert.True(
                ratio >= minimum,
                $"{themeName}: {fore} on {back} is {ratio:F2}:1, needs {minimum}:1");
        }
    }

    private static double Ratio(Color fore, Color back)
    {
        double bright = Math.Max(Luminance(fore), Luminance(back));
        double dim = Math.Min(Luminance(fore), Luminance(back));
        return (bright + 0.05) / (dim + 0.05);
    }

    private static double Luminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte channel)
    {
        double scaled = channel / 255.0;
        return scaled <= 0.04045 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }
}
