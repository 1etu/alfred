using System.Reflection;
using Alfred.Theme;
using Alfred.Theme.Defaults;
using Xunit;

namespace Alfred.Theme.Tests.Defaults;

public class DefaultThemesTests
{
    private static IReadOnlyList<string> DeclaredKeys => [.. typeof(ThemeKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(constant => constant.IsLiteral)
        .Select(constant => (string)constant.GetRawConstantValue()!)];

    [Fact]
    public void LightAndDarkExposeTheSameKeys()
    {
        Assert.Equal(
            DefaultThemes.Light.Colors.Keys.Order(),
            DefaultThemes.Dark.Colors.Keys.Order());
    }

    [Fact]
    public void EveryThemeKeyHasAColour()
    {
        Assert.Equal(DeclaredKeys.Order(), DefaultThemes.Light.Colors.Keys.Order());
        Assert.Equal(DeclaredKeys.Order(), DefaultThemes.Dark.Colors.Keys.Order());
    }

    [Fact]
    public void VariantsMatchTheirNames()
    {
        Assert.Equal(ThemeVariant.Light, DefaultThemes.Light.Variant);
        Assert.Equal(ThemeVariant.Dark, DefaultThemes.Dark.Variant);
        Assert.False(DefaultThemes.Light.IsDark);
        Assert.True(DefaultThemes.Dark.IsDark);
    }
}
