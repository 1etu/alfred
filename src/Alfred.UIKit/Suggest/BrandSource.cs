namespace Alfred.UIKit.Suggest;

public sealed class BrandSource : ISuggestionSource
{
    public IReadOnlyList<Suggestion> Suggest(string query, int limit) =>
        [.. BrandCatalog.Search(query, limit)
            .Select(brand => new Suggestion(brand.Name, null, brand.Slug, brand))];
}
