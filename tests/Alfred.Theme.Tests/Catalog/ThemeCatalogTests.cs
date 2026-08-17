using System.Windows.Media;
using Alfred.Theme;
using Alfred.Theme.Catalog;
using Alfred.Theme.Defaults;
using Xunit;

namespace Alfred.Theme.Tests.Catalog;

public class ThemeCatalogTests
{
    [Fact]
    public void ResolveFindsBuiltInsByNameIgnoringCase()
    {
        Assert.Same(DefaultThemes.Light, ThemeCatalog.Resolve("light"));
        Assert.Same(DefaultThemes.Dark, ThemeCatalog.Resolve("DARK"));
    }

    [Fact]
    public void ResolveFallsBackToADefaultForUnknownNames()
    {
        Theme resolved = ThemeCatalog.Resolve("does-not-exist");

        Assert.True(resolved == DefaultThemes.Light || resolved == DefaultThemes.Dark);
    }

    [Fact]
    public void ResolveSystemReturnsADefault()
    {
        Theme resolved = ThemeCatalog.Resolve(ThemeCatalog.SystemSelection);

        Assert.True(resolved == DefaultThemes.Light || resolved == DefaultThemes.Dark);
        Assert.Same(resolved, ThemeCatalog.Resolve(null));
    }

    [Fact]
    public void RegisterMakesAThemeResolvable()
    {
        Theme midnight = DefaultThemes.Dark.Extend("Midnight", new Dictionary<string, Color>
        {
            [ThemeKeys.Accent] = Color.FromRgb(0x8B, 0x5C, 0xF6),
        });

        ThemeCatalog.Register(midnight);

        Assert.Same(midnight, ThemeCatalog.Resolve("Midnight"));
        Assert.Same(midnight, ThemeCatalog.Find("midnight"));
        Assert.Contains(midnight, ThemeCatalog.All);
    }
}
