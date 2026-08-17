using System.Globalization;
using Alfred.Core.Time;

namespace Alfred.UIKit.Suggest;

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
