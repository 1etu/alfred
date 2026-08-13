using Alfred.Core.Ledger;
using Xunit;

namespace Alfred.Core.Tests.Ledger;

public sealed class ScheduleTests
{
    [Fact]
    public void OnceYieldsInsideWindowOnly()
    {
        Schedule schedule = Schedule.Once(new DateOnly(2026, 8, 11));

        Assert.Single(schedule.Occurrences(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
        Assert.Empty(schedule.Occurrences(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public void WeeklyStartsAtFirstOccurrenceInsideWindow()
    {
        Schedule schedule = Schedule.Every(Cadence.Weekly, new DateOnly(2026, 8, 3));

        List<DateOnly> occurrences = [.. schedule.Occurrences(new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 31))];

        Assert.Equal([new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 24), new DateOnly(2026, 8, 31)], occurrences);
    }

    [Fact]
    public void MonthlyClampsToShortMonths()
    {
        Schedule schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 1, 31));

        List<DateOnly> occurrences = [.. schedule.Occurrences(new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 30))];

        Assert.Equal(
            [new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30)],
            occurrences);
    }

    [Fact]
    public void MonthlyNeverYieldsBeforeAnchor()
    {
        Schedule schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 11));

        Assert.Empty(schedule.Occurrences(new DateOnly(2026, 1, 1), new DateOnly(2026, 8, 10)));
    }

    [Fact]
    public void YearlyClampsLeapAnchor()
    {
        Schedule schedule = Schedule.Every(Cadence.Yearly, new DateOnly(2024, 2, 29));

        List<DateOnly> occurrences = [.. schedule.Occurrences(new DateOnly(2025, 1, 1), new DateOnly(2028, 12, 31))];

        Assert.Equal(
            [new DateOnly(2025, 2, 28), new DateOnly(2026, 2, 28), new DateOnly(2027, 2, 28), new DateOnly(2028, 2, 29)],
            occurrences);
    }

    [Fact]
    public void EveryRejectsNoneCadence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule.Every(Cadence.None, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void EmptyWhenWindowInverted()
    {
        Schedule schedule = Schedule.Every(Cadence.Weekly, new DateOnly(2026, 1, 1));

        Assert.Empty(schedule.Occurrences(new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 1)));
    }
}
