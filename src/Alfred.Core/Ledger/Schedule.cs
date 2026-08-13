using System.Text.Json.Serialization;

namespace Alfred.Core.Ledger;

public sealed record Schedule
{
    [JsonConstructor]
    private Schedule(Cadence cadence, DateOnly anchor)
    {
        Cadence = cadence;
        Anchor = anchor;
    }

    public Cadence Cadence { get; }

    public DateOnly Anchor { get; }

    public bool IsRecurring => Cadence != Cadence.None;

    public static Schedule Once(DateOnly on) => new(Cadence.None, on);

    public static Schedule Every(Cadence cadence, DateOnly anchor)
    {
        if (cadence == Cadence.None)
        {
            throw new ArgumentOutOfRangeException(nameof(cadence));
        }

        return new Schedule(cadence, anchor);
    }

    public IEnumerable<DateOnly> Occurrences(DateOnly from, DateOnly until)
    {
        if (until < from)
        {
            yield break;
        }

        switch (Cadence)
        {
            case Cadence.None:
                if (Anchor >= from && Anchor <= until)
                {
                    yield return Anchor;
                }

                break;

            case Cadence.Weekly:
                foreach (DateOnly occurrence in Weekly(from, until))
                {
                    yield return occurrence;
                }

                break;

            case Cadence.Monthly:
                foreach (DateOnly occurrence in Monthly(from, until))
                {
                    yield return occurrence;
                }

                break;

            case Cadence.Yearly:
                foreach (DateOnly occurrence in Yearly(from, until))
                {
                    yield return occurrence;
                }

                break;
        }
    }

    private IEnumerable<DateOnly> Weekly(DateOnly from, DateOnly until)
    {
        DateOnly start = Anchor;

        if (from > Anchor)
        {
            int daysBehind = from.DayNumber - Anchor.DayNumber;
            start = Anchor.AddDays((daysBehind + 6) / 7 * 7);
        }

        for (DateOnly occurrence = start; occurrence <= until; occurrence = occurrence.AddDays(7))
        {
            yield return occurrence;
        }
    }

    private IEnumerable<DateOnly> Monthly(DateOnly from, DateOnly until)
    {
        DateOnly floor = from > Anchor ? from : Anchor;
        int year = floor.Year;
        int month = floor.Month;

        while (true)
        {
            DateOnly occurrence = ClampToMonth(year, month, Anchor.Day);

            if (occurrence > until)
            {
                yield break;
            }

            if (occurrence >= floor)
            {
                yield return occurrence;
            }

            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
        }
    }

    private IEnumerable<DateOnly> Yearly(DateOnly from, DateOnly until)
    {
        DateOnly floor = from > Anchor ? from : Anchor;

        for (int year = floor.Year; ; year++)
        {
            DateOnly occurrence = ClampToMonth(year, Anchor.Month, Anchor.Day);

            if (occurrence > until)
            {
                yield break;
            }

            if (occurrence >= floor)
            {
                yield return occurrence;
            }
        }
    }

    private static DateOnly ClampToMonth(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
