namespace Alfred.UIKit.Suggest;

public interface ISuggestionSource
{
    IReadOnlyList<Suggestion> Suggest(string query, int limit);
}
