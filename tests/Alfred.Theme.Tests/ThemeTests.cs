using System.Windows.Media;
using Alfred.Theme;
using Alfred.Theme.Defaults;
using Xunit;

namespace Alfred.Theme.Tests;

public class ThemeTests
{
    private static readonly Color Violet = Color.FromRgb(0x8B, 0x5C, 0xF6);

    [Fact]
    public void ExtendOverridesColour()
    {
        Theme extended = DefaultThemes.Dark.Extend("Midnight", new Dictionary<string, Color>
        {
            [ThemeKeys.Accent] = Violet,
        });

        Assert.Equal("Midnight", extended.Name);
        Assert.Equal(ThemeVariant.Dark, extended.Variant);
        Assert.Equal(Violet, extended.Colors[ThemeKeys.Accent]);
    }

    [Fact]
    public void ExtendKeepsUntouchedColours()
    {
        Theme extended = DefaultThemes.Dark.Extend("Midnight", new Dictionary<string, Color>
        {
            [ThemeKeys.Accent] = Violet,
        });

        foreach (string key in DefaultThemes.Dark.Colors.Keys.Where(key => key != ThemeKeys.Accent))
        {
            Assert.Equal(DefaultThemes.Dark.Colors[key], extended.Colors[key]);
        }
    }

    [Fact]
    public void ExtendAddsIconColours()
    {
        Theme extended = DefaultThemes.Light.Extend("Tinted", iconColors: new Dictionary<string, Color>
        {
            ["TodosIcon"] = Violet,
        });

        Assert.Equal(Violet, extended.IconColors["TodosIcon"]);
        Assert.Empty(DefaultThemes.Light.IconColors);
    }

    [Fact]
    public void ExtendLeavesBaseUnchanged()
    {
        Color original = DefaultThemes.Light.Colors[ThemeKeys.Accent];

        DefaultThemes.Light.Extend("Copy", new Dictionary<string, Color>
        {
            [ThemeKeys.Accent] = Violet,
        });

        Assert.Equal(original, DefaultThemes.Light.Colors[ThemeKeys.Accent]);
    }

    [Fact]
    public void ExtendRejectsBlankName() =>
        Assert.ThrowsAny<ArgumentException>(() => DefaultThemes.Light.Extend(" "));
}
