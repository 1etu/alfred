using Alfred.Core.Time;
using Xunit;

namespace Alfred.Core.Tests.Time;

public sealed class DateHintsTests
{
    private static readonly DateOnly Today = new(2026, 8, 13);

    [Fact]
    public void NumberSuggestsDayOfMonthAndRelativeDays()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("30", Today);

        Assert.Contains(hints, hint => hint.Date == new DateOnly(2026, 8, 30) && hint.Label == "30th of August");
        Assert.Contains(hints, hint => hint.Date == new DateOnly(2026, 9, 12) && hint.Label == "In 30 days");
    }

    [Fact]
    public void PastDayOfMonthRollsToNextMonth()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("11", Today);

        Assert.Contains(hints, hint => hint.Date == new DateOnly(2026, 9, 11));
    }

    [Fact]
    public void LargeNumberOnlySuggestsRelativeDays()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("45", Today);

        Assert.Single(hints);
        Assert.Equal(Today.AddDays(45), hints[0].Date);
    }

    [Fact]
    public void WeekdayPrefixSuggestsNextOccurrence()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("fri", Today);

        Assert.Contains(hints, hint => hint.Date == new DateOnly(2026, 8, 14) && hint.Label == "Friday");
    }

    [Fact]
    public void SameWeekdayMeansNextWeek()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("thu", Today);

        Assert.Contains(hints, hint => hint.Date == new DateOnly(2026, 8, 20));
    }

    [Fact]
    public void ExplicitDayMonthParsesInEitherOrder()
    {
        Assert.Contains(DateHints.Suggest("20 aug", Today), hint => hint.Date == new DateOnly(2026, 8, 20));
        Assert.Contains(DateHints.Suggest("aug 20", Today), hint => hint.Date == new DateOnly(2026, 8, 20));
        Assert.Contains(DateHints.Suggest("20.8", Today), hint => hint.Date == new DateOnly(2026, 8, 20));
    }

    [Fact]
    public void PastExplicitDateRollsToNextYear()
    {
        Assert.Contains(DateHints.Suggest("11 aug", Today), hint => hint.Date == new DateOnly(2027, 8, 11));
    }

    [Fact]
    public void RelativePhrasesParse()
    {
        Assert.Contains(DateHints.Suggest("in 2 weeks", Today), hint => hint.Date == Today.AddDays(14));
        Assert.Contains(DateHints.Suggest("2 w", Today), hint => hint.Date == Today.AddDays(14));
        Assert.Contains(DateHints.Suggest("in 3 months", Today), hint => hint.Date == Today.AddMonths(3));
        Assert.Contains(DateHints.Suggest("next m", Today), hint => hint.Date == new DateOnly(2026, 9, 1));
        Assert.Contains(DateHints.Suggest("weekend", Today), hint => hint.Date == new DateOnly(2026, 8, 15));
        Assert.Contains(DateHints.Suggest("eom", Today), hint => hint.Date == new DateOnly(2026, 8, 31));
    }

    [Fact]
    public void EmptyInputSuggestsDefaults()
    {
        IReadOnlyList<DateHint> hints = DateHints.Suggest("", Today);

        Assert.Equal(3, hints.Count);
        Assert.Equal(Today, hints[0].Date);
    }
}
