using System.Windows.Media;
using Alfred.Theme;
using Alfred.Theme.Defaults;
using Alfred.Theme.Storage;
using Xunit;

namespace Alfred.Theme.Tests.Storage;

public class ThemeJsonTests
{
    private static readonly Color Violet = Color.FromRgb(0x8B, 0x5C, 0xF6);

    [Fact]
    public void RoundTripPreservesOverrides()
    {
        Theme original = DefaultThemes.Dark.Extend(
            "Midnight",
            new Dictionary<string, Color> { [ThemeKeys.Accent] = Violet },
            new Dictionary<string, Color> { ["TodosIcon"] = Violet });

        Theme? restored = ThemeJson.Read(ThemeJson.Write(original));

        Assert.NotNull(restored);
        Assert.Equal("Midnight", restored.Name);
        Assert.Equal(ThemeVariant.Dark, restored.Variant);
        Assert.Equal(Violet, restored.Colors[ThemeKeys.Accent]);
        Assert.Equal(Violet, restored.IconColors["TodosIcon"]);
        Assert.Equal(DefaultThemes.Dark.Colors[ThemeKeys.SheetBackground], restored.Colors[ThemeKeys.SheetBackground]);
    }

    [Fact]
    public void WriteOmitsInheritedColours()
    {
        string json = ThemeJson.Write(DefaultThemes.Light.Extend("Copy"));

        Assert.DoesNotContain("colors", json, StringComparison.Ordinal);
        Assert.DoesNotContain(ThemeKeys.SheetBackground, json, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadDefaultsToTheLightBase()
    {
        Theme? restored = ThemeJson.Read("""{ "name": "Plain" }""");

        Assert.NotNull(restored);
        Assert.Equal(ThemeVariant.Light, restored.Variant);
        Assert.Equal(DefaultThemes.Light.Colors, restored.Colors);
    }

    [Fact]
    public void ReadRejectsInvalidJson()
    {
        Assert.Null(ThemeJson.Read("not json"));
        Assert.Null(ThemeJson.Read("""{ "base": "Dark" }"""));
    }

    [Fact]
    public void ReadRejectsMalformedColours()
    {
        Assert.Null(ThemeJson.Read("""{ "name": "Broken", "colors": { "Accent": "purple" } }"""));
        Assert.Null(ThemeJson.Read("""{ "name": "Broken", "colors": { "Accent": "#12345" } }"""));
    }

    [Fact]
    public void RoundTripKeepsTranslucentColours()
    {
        Color translucent = Color.FromArgb(0x24, 0x35, 0x74, 0xF0);
        Theme original = DefaultThemes.Dark.Extend(
            "Glassy",
            new Dictionary<string, Color> { [ThemeKeys.AccentSoft] = translucent });

        Theme? restored = ThemeJson.Read(ThemeJson.Write(original));

        Assert.NotNull(restored);
        Assert.Equal(translucent, restored.Colors[ThemeKeys.AccentSoft]);
    }
}
