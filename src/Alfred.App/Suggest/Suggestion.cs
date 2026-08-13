namespace Alfred.App.Suggest;

public sealed record Suggestion(string Primary, string? Secondary, string? BrandSlug, object? Value);

public interface ISuggestionSource
{
    IReadOnlyList<Suggestion> Suggest(string query, int limit);
}
