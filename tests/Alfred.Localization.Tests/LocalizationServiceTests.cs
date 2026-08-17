using Xunit;

namespace Alfred.Localization.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void TextReadsTheActiveLanguage()
    {
        LocalizationService.Apply(LanguageCatalog.Resolve("tr"));

        try
        {
            Assert.Equal("Bugün", LocalizationService.Text(LocalizationKeys.NavToday));
        }
        finally
        {
            LocalizationService.Apply(LanguageCatalog.English);
        }

        Assert.Equal("Today", LocalizationService.Text(LocalizationKeys.NavToday));
    }

    [Fact]
    public void TextFallsBackToTheKeyForUnknownKeys()
    {
        Assert.Equal("Loc.Does.Not.Exist", LocalizationService.Text("Loc.Does.Not.Exist"));
    }

    [Fact]
    public void TextFormatsArguments()
    {
        Assert.Equal(
            "Already used by Quick capture",
            LocalizationService.Text(LocalizationKeys.ShortcutUsedBy, "Quick capture"));
    }

    [Fact]
    public void ApplyRaisesChanged()
    {
        Language? observed = null;
        void Observe(object? sender, Language language) => observed = language;

        LocalizationService.Changed += Observe;

        try
        {
            LocalizationService.Apply(LanguageCatalog.English);
        }
        finally
        {
            LocalizationService.Changed -= Observe;
        }

        Assert.Same(LanguageCatalog.English, observed);
    }
}
