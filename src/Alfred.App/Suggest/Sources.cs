using System.Globalization;
using Alfred.Core.Ledger;
using Alfred.Core.Search;
using Alfred.Core.Time;

namespace Alfred.App.Suggest;

public sealed class BrandSource : ISuggestionSource
{
    public IReadOnlyList<Suggestion> Suggest(string query, int limit) =>
        [.. BrandCatalog.Search(query, limit)
            .Select(brand => new Suggestion(brand.Name, null, brand.Slug, brand))];
}

public sealed class DateSource : ISuggestionSource
{
    private readonly Func<DateOnly> _today;

    public DateSource(Func<DateOnly>? today = null)
    {
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
    }

    public IReadOnlyList<Suggestion> Suggest(string query, int limit) =>
        [.. DateHints.Suggest(query, _today())
            .Take(limit)
            .Select(hint => new Suggestion(
                hint.Label,
                hint.Date.ToString("ddd, d MMM", CultureInfo.InvariantCulture),
                null,
                hint.Date))];
}

public sealed class CategorySource : ISuggestionSource
{
    private readonly CashFlow? _flow;

    public CategorySource(CashFlow? flow = null)
    {
        _flow = flow;
    }

    public IReadOnlyList<Suggestion> Suggest(string query, int limit) =>
        [.. Categories.All
            .Where(category => _flow is null || category.Flow == _flow)
            .Select(category => (Category: category, Score: FuzzyMatcher.Score(query, category.Name)))
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .Take(limit)
            .Select(entry => new Suggestion(entry.Category.Name, null, null, entry.Category))];
}

public sealed class CompositeSource : ISuggestionSource
{
    private readonly IReadOnlyList<ISuggestionSource> _sources;

    public CompositeSource(params IReadOnlyList<ISuggestionSource> sources)
    {
        _sources = sources;
    }

    public IReadOnlyList<Suggestion> Suggest(string query, int limit)
    {
        List<Suggestion> merged = [];

        foreach (ISuggestionSource source in _sources)
        {
            merged.AddRange(source.Suggest(query, limit));
            if (merged.Count >= limit)
            {
                break;
            }
        }

        return merged.Count <= limit ? merged : [.. merged.Take(limit)];
    }
}
