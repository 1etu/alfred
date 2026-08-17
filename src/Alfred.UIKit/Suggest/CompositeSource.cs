namespace Alfred.UIKit.Suggest;

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
