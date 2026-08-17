using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows;
using Alfred.UIKit.Icons;

namespace Alfred.UIKit;

public sealed class UIKitResources : ResourceDictionary
{
    private const string BamlExtension = ".baml";

    private static readonly string[] FoundationDictionaries =
    [
        "styles/typography.xaml",
        "styles/glyphs.xaml",
    ];

    public UIKitResources()
    {
        foreach (string dictionary in FoundationDictionaries)
        {
            Merge(dictionary);
        }

        foreach (string dictionary in DiscoverDictionaries())
        {
            Merge(dictionary);
        }

        IconLibrary.Track(this);
    }

    private void Merge(string dictionary) => MergedDictionaries.Add(new ResourceDictionary
    {
        Source = new Uri($"/Alfred.UIKit;component/{dictionary}", UriKind.Relative),
    });

    private static List<string> DiscoverDictionaries()
    {
        List<string> discovered = [];
        Assembly assembly = typeof(UIKitResources).Assembly;

        using Stream? manifest = assembly.GetManifestResourceStream("Alfred.UIKit.g.resources");
        if (manifest is null)
        {
            return discovered;
        }

        using ResourceReader reader = new(manifest);

        foreach (DictionaryEntry entry in reader)
        {
            if (entry.Key is not string key || !IsStyleDictionary(key))
            {
                continue;
            }

            string dictionary = string.Concat(key.AsSpan(0, key.Length - BamlExtension.Length), ".xaml");

            if (!FoundationDictionaries.Contains(dictionary))
            {
                discovered.Add(dictionary);
            }
        }

        discovered.Sort(StringComparer.Ordinal);
        return discovered;
    }

    private static bool IsStyleDictionary(string key) =>
        key.EndsWith(BamlExtension, StringComparison.Ordinal) &&
        (key.StartsWith("styles/", StringComparison.Ordinal) ||
         key.Contains("/styles/", StringComparison.Ordinal));
}
