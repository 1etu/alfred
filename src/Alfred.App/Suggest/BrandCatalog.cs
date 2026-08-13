using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alfred.Core.Search;

namespace Alfred.App.Suggest;

public sealed record BrandPath(
    [property: JsonPropertyName("d")] string Data,
    [property: JsonPropertyName("fill")] string? Fill);

public sealed record Brand(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("hex")] string Hex,
    [property: JsonPropertyName("aliases")] IReadOnlyList<string>? Aliases,
    [property: JsonPropertyName("paths")] IReadOnlyList<BrandPath> Paths);

public static class BrandCatalog
{
    private sealed record Catalog([property: JsonPropertyName("brands")] IReadOnlyList<Brand> Brands);

    private static readonly Lazy<IReadOnlyList<Brand>> Loaded = new(Load);
    private static readonly Lazy<Dictionary<string, Brand>> BySlug = new(
        () => Loaded.Value.ToDictionary(brand => brand.Slug, StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<Brand> All => Loaded.Value;

    public static Brand? Find(string slug) =>
        BySlug.Value.TryGetValue(slug, out Brand? brand) ? brand : null;

    public static IReadOnlyList<Brand> Search(string query, int limit)
    {
        List<(Brand Brand, int Score)> scored = [];

        foreach (Brand brand in Loaded.Value)
        {
            int score = FuzzyMatcher.Score(query, brand.Name);

            if (brand.Aliases is not null)
            {
                foreach (string alias in brand.Aliases)
                {
                    score = Math.Max(score, FuzzyMatcher.Score(query, alias) - 1);
                }
            }

            if (score > 0)
            {
                scored.Add((brand, score));
            }
        }

        return [.. scored
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Brand.Name.Length)
            .ThenBy(entry => entry.Brand.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(entry => entry.Brand)];
    }

    private static IReadOnlyList<Brand> Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream? packed = assembly.GetManifestResourceStream("Alfred.App.Resources.Brands.brands.json.gz");
        if (packed is not null)
        {
            using GZipStream unzipped = new(packed, CompressionMode.Decompress);
            return JsonSerializer.Deserialize<Catalog>(unzipped)?.Brands ?? [];
        }

        using Stream? plain = assembly.GetManifestResourceStream("Alfred.App.Resources.Brands.brands.json");
        if (plain is not null)
        {
            return JsonSerializer.Deserialize<Catalog>(plain)?.Brands ?? [];
        }

        return [];
    }
}
