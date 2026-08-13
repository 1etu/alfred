using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Alfred.Widgets.Cards;

internal static class CardTemplate
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

    public static string Read(string name) => Cache.GetOrAdd(name, ReadEmbeddedCard);

    private static string ReadEmbeddedCard(string name)
    {
        var assembly = typeof(CardTemplate).Assembly;
        var resourceName = $"Alfred.Widgets.Cards.{name}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The card template '{resourceName}' is not embedded in {assembly.GetName().Name}.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
