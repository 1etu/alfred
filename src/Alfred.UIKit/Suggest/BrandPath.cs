using System.Text.Json.Serialization;

namespace Alfred.UIKit.Suggest;

public sealed record BrandPath(
    [property: JsonPropertyName("d")] string Data,
    [property: JsonPropertyName("fill")] string? Fill);
