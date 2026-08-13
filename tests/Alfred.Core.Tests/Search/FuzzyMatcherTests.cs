using Alfred.Core.Search;
using Xunit;

namespace Alfred.Core.Tests.Search;

public sealed class FuzzyMatcherTests
{
    [Fact]
    public void PrefixBeatsContainsBeatsSubsequence()
    {
        int prefix = FuzzyMatcher.Score("net", "Netflix");
        int contains = FuzzyMatcher.Score("flix", "Netflix");
        int subsequence = FuzzyMatcher.Score("nfx", "Netflix");

        Assert.True(prefix > contains);
        Assert.True(contains > subsequence);
        Assert.True(subsequence > 0);
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        Assert.True(FuzzyMatcher.Score("NETF", "netflix") > 0);
    }

    [Fact]
    public void NoMatchReturnsNegative()
    {
        Assert.Equal(-1, FuzzyMatcher.Score("xyz", "Netflix"));
    }

    [Fact]
    public void EmptyQueryMatchesEverything()
    {
        Assert.Equal(1, FuzzyMatcher.Score("", "anything"));
    }
}
