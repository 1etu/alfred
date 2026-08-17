using Alfred.Core.Ledger;
using Alfred.Core.Search;

namespace Alfred.UIKit.Suggest;

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
