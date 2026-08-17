using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Alfred.Localization.Tests;

public partial class LanguageCatalogTests
{
    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z]{2,4})?$")]
    private static partial Regex LanguageCode();

    private static IReadOnlyList<string> DeclaredKeys => [.. typeof(LocalizationKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(constant => constant.IsLiteral)
        .Select(constant => (string)constant.GetRawConstantValue()!)];

    [Fact]
    public void EnglishCoversEveryDeclaredKey()
    {
        Assert.Equal(DeclaredKeys.Order(), LanguageCatalog.English.Strings.Keys.Order());
    }

    [Fact]
    public void EveryLanguageUsesOnlyKnownKeys()
    {
        foreach (Language language in LanguageCatalog.All)
        {
            foreach (string key in language.Strings.Keys)
            {
                Assert.True(
                    LanguageCatalog.English.Strings.ContainsKey(key),
                    $"{language.Code}.json has unknown key '{key}'");
            }
        }
    }

    [Fact]
    public void EveryLanguageHasANameAndAValidCode()
    {
        foreach (Language language in LanguageCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(language.NativeName));
            Assert.Matches(LanguageCode(), language.Code);
        }
    }

    [Fact]
    public void EveryLanguageKeepsFormatPlaceholders()
    {
        foreach (Language language in LanguageCatalog.All)
        {
            foreach ((string key, string english) in LanguageCatalog.English.Strings)
            {
                if (!english.Contains("{0}", StringComparison.Ordinal) ||
                    !language.Strings.TryGetValue(key, out string? translated))
                {
                    continue;
                }

                Assert.True(
                    translated.Contains("{0}", StringComparison.Ordinal),
                    $"{language.Code}.json dropped the placeholder in '{key}'");
            }
        }
    }

    [Fact]
    public void EnglishComesFirstAndResolvesAsFallback()
    {
        Assert.Equal("en", LanguageCatalog.All[0].Code);
        Assert.Same(LanguageCatalog.English, LanguageCatalog.Resolve("unknown"));
        Assert.Same(LanguageCatalog.English, LanguageCatalog.Resolve(null));
        Assert.Equal("tr", LanguageCatalog.Resolve("TR").Code);
    }
}
