namespace Alfred.UIKit.Suggest;

public sealed record Suggestion(string Primary, string? Secondary, string? BrandSlug, object? Value);
