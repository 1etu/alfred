using System.Collections.ObjectModel;
using System.Windows;

namespace Alfred.Localization;

public static class LocalizationService
{
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> MergedByCode = [];
    private static ResourceDictionary? _applied;

    public static Language Current { get; private set; } = LanguageCatalog.English;

    public static event EventHandler<Language>? Changed;

    public static void Apply(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        Current = language;
        Swap(BuildStrings(language));
        Changed?.Invoke(null, language);
    }

    public static string Text(string key) =>
        Merged(Current).TryGetValue(key, out string? value) ? value : key;

    public static string Text(string key, params object?[] arguments) =>
        string.Format(Current.Culture, Text(key), arguments);

    private static IReadOnlyDictionary<string, string> Merged(Language language)
    {
        if (MergedByCode.TryGetValue(language.Code, out IReadOnlyDictionary<string, string>? merged))
        {
            return merged;
        }

        Dictionary<string, string> combined = new(LanguageCatalog.English.Strings);

        foreach ((string key, string value) in language.Strings)
        {
            combined[key] = value;
        }

        ReadOnlyDictionary<string, string> frozen = new(combined);
        MergedByCode[language.Code] = frozen;
        return frozen;
    }

    private static void Swap(ResourceDictionary strings)
    {
        if (Application.Current is null)
        {
            return;
        }

        Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;
        int index = _applied is null ? -1 : merged.IndexOf(_applied);

        if (index < 0)
        {
            merged.Add(strings);
        }
        else
        {
            merged[index] = strings;
        }

        _applied = strings;
    }

    private static ResourceDictionary BuildStrings(Language language)
    {
        ResourceDictionary strings = new();

        foreach ((string key, string value) in Merged(language))
        {
            strings[key] = value;
        }

        return strings;
    }
}
