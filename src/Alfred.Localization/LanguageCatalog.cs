using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alfred.Localization;

public static class LanguageCatalog
{
    private const string ResourcePrefix = "Alfred.Localization.Languages.";
    private const string ResourceSuffix = ".json";
    private const string EnglishCode = "en";

    private sealed record LanguageFile(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("strings")] Dictionary<string, string>? Strings);

    private static readonly Lazy<IReadOnlyList<Language>> Loaded = new(Load);

    public static IReadOnlyList<Language> All => Loaded.Value;

    public static Language English => Find(EnglishCode)
        ?? throw new InvalidOperationException("The English language file is missing.");

    public static Language? Find(string? code)
    {
        foreach (Language language in All)
        {
            if (string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        return null;
    }

    public static Language Resolve(string? code) => Find(code) ?? English;

    private static List<Language> Load()
    {
        Assembly assembly = typeof(LanguageCatalog).Assembly;
        List<Language> languages = [];

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            string code = resource[ResourcePrefix.Length..^ResourceSuffix.Length];
            languages.Add(Read(assembly, resource, code));
        }

        languages.Sort((first, second) => first.Code == EnglishCode ? -1
            : second.Code == EnglishCode ? 1
            : string.Compare(first.Code, second.Code, StringComparison.Ordinal));

        return languages;
    }

    private static Language Read(Assembly assembly, string resource, string code)
    {
        using Stream? stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Language file '{resource}' could not be opened.");

        LanguageFile? file;

        try
        {
            file = JsonSerializer.Deserialize<LanguageFile>(stream);
        }
        catch (JsonException failure)
        {
            throw new InvalidOperationException($"Language file '{code}.json' is not valid JSON.", failure);
        }

        if (file?.Name is not { Length: > 0 } name || file.Strings is not { Count: > 0 } strings)
        {
            throw new InvalidOperationException($"Language file '{code}.json' needs a name and at least one string.");
        }

        return new Language(code, name, strings);
    }
}
