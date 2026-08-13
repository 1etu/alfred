namespace Alfred.Core.Search;

public static class FuzzyMatcher
{
    public static int Score(string query, string candidate)
    {
        if (string.IsNullOrEmpty(query))
        {
            return 1;
        }

        if (string.IsNullOrEmpty(candidate))
        {
            return -1;
        }

        if (candidate.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1000 - (candidate.Length - query.Length);
        }

        int wordStart = candidate.IndexOf(' ' + query, StringComparison.OrdinalIgnoreCase);
        if (wordStart >= 0)
        {
            return 800 - wordStart;
        }

        int contains = candidate.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (contains >= 0)
        {
            return 600 - contains;
        }

        return Subsequence(query, candidate);
    }

    private static int Subsequence(string query, string candidate)
    {
        int gaps = 0;
        int position = 0;

        foreach (char wanted in query)
        {
            int found = candidate.IndexOf(char.ToUpperInvariant(wanted), position);
            if (found < 0)
            {
                found = candidate.IndexOf(char.ToLowerInvariant(wanted), position);
            }

            if (found < 0)
            {
                return -1;
            }

            gaps += found - position;
            position = found + 1;
        }

        return 400 - gaps;
    }
}
