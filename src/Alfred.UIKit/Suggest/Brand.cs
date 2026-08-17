using System.Text.Json.Serialization;

namespace Alfred.UIKit.Suggest;

public sealed record Brand(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("hex")] string Hex,
    [property: JsonPropertyName("aliases")] IReadOnlyList<string>? Aliases,
    [property: JsonPropertyName("paths")] IReadOnlyList<BrandPath> Paths);
