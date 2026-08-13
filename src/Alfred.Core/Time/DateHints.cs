using System.Globalization;

namespace Alfred.Core.Time;

public sealed record DateHint(DateOnly Date, string Label);

public static class DateHints
{
    private static readonly string[] MonthNames = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;

    public static IReadOnlyList<DateHint> Suggest(string input, DateOnly today)
    {
        string text = input.Trim();
        List<DateHint> hints = [];

        if (text.Length == 0)
        {
            hints.Add(new DateHint(today, "Today"));
            hints.Add(new DateHint(today.AddDays(1), "Tomorrow"));
            hints.Add(new DateHint(NextWeekday(today, DayOfWeek.Monday), "Next week"));
            return hints;
        }

        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number > 0)
        {
            if (number <= 31)
            {
                DateOnly dayOfMonth = NextDayOfMonth(today, number);
                hints.Add(new DateHint(dayOfMonth, $"{Ordinal(number)} of {MonthNames[dayOfMonth.Month - 1]}"));
            }

            if (number <= 365)
            {
                hints.Add(new DateHint(today.AddDays(number), $"In {number} days"));
            }

            return hints;
        }

        if (Matches(text, "today"))
        {
            hints.Add(new DateHint(today, "Today"));
        }

        if (Matches(text, "tomorrow") || Matches(text, "tmr"))
        {
            hints.Add(new DateHint(today.AddDays(1), "Tomorrow"));
        }

        if (Matches(text, "next week"))
        {
            hints.Add(new DateHint(NextWeekday(today, DayOfWeek.Monday), "Next week"));
        }

        if (Matches(text, "next month"))
        {
            DateOnly next = today.AddMonths(1);
            hints.Add(new DateHint(new DateOnly(next.Year, next.Month, 1), "Next month"));
        }

        if (Matches(text, "weekend") || Matches(text, "this weekend"))
        {
            hints.Add(new DateHint(NextWeekday(today, DayOfWeek.Saturday), "Weekend"));
        }

        if (Matches(text, "end of month") || Matches(text, "eom"))
        {
            hints.Add(new DateHint(
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                "End of month"));
        }

        if (TryRelative(text, today, out DateHint relative))
        {
            hints.Add(relative);
        }

        if (text.Length >= 2)
        {
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                if (day.ToString().StartsWith(text, StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new DateHint(NextWeekday(today, day), day.ToString()));
                }
            }
        }

        if (TryExplicitDate(text, today, out DateOnly explicitDate))
        {
            hints.Add(new DateHint(explicitDate, $"{explicitDate.Day} {MonthNames[explicitDate.Month - 1]}"));
        }

        return hints;
    }

    private static bool Matches(string text, string phrase) =>
        phrase.StartsWith(text, StringComparison.OrdinalIgnoreCase);

    private static bool TryRelative(string text, DateOnly today, out DateHint hint)
    {
        hint = default!;
        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int start = parts.Length > 0 && parts[0].Equals("in", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (parts.Length - start != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[start], NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count is < 1 or > 24)
        {
            return false;
        }

        string unit = parts[start + 1];

        if (Unit(unit, "days"))
        {
            hint = new DateHint(today.AddDays(count), $"In {count} days");
        }
        else if (Unit(unit, "weeks"))
        {
            hint = new DateHint(today.AddDays(count * 7), count == 1 ? "In 1 week" : $"In {count} weeks");
        }
        else if (Unit(unit, "months"))
        {
            hint = new DateHint(today.AddMonths(count), count == 1 ? "In 1 month" : $"In {count} months");
        }
        else
        {
            return false;
        }

        return true;
    }

    private static bool Unit(string text, string full) =>
        full.StartsWith(text, StringComparison.OrdinalIgnoreCase) && text.Length >= 1;

    private static bool TryExplicitDate(string text, DateOnly today, out DateOnly date)
    {
        date = default;
        string[] parts = text.Split([' ', '.', '/'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        int day = 0;
        int month = 0;

        foreach (string part in parts)
        {
            if (int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                if (day == 0)
                {
                    day = value;
                }
                else
                {
                    month = value;
                }
            }
            else if (part.Length >= 3 && month == 0)
            {
                for (int index = 0; index < 12; index++)
                {
                    if (MonthNames[index].StartsWith(part, StringComparison.OrdinalIgnoreCase))
                    {
                        month = index + 1;
                        break;
                    }
                }
            }
        }

        if (day is < 1 or > 31 || month is < 1 or > 12 || day > DateTime.DaysInMonth(today.Year, month))
        {
            return false;
        }

        date = new DateOnly(today.Year, month, day);
        if (date < today)
        {
            date = new DateOnly(today.Year + 1, month, Math.Min(day, DateTime.DaysInMonth(today.Year + 1, month)));
        }

        return true;
    }

    private static DateOnly NextWeekday(DateOnly today, DayOfWeek day)
    {
        int ahead = ((int)day - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(ahead == 0 ? 7 : ahead);
    }

    private static DateOnly NextDayOfMonth(DateOnly today, int day)
    {
        DateOnly candidate = new(today.Year, today.Month, Math.Min(day, DateTime.DaysInMonth(today.Year, today.Month)));

        if (candidate < today)
        {
            DateOnly next = today.AddMonths(1);
            candidate = new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)));
        }

        return candidate;
    }

    private static string Ordinal(int number) => number switch
    {
        1 or 21 or 31 => $"{number}st",
        2 or 22 => $"{number}nd",
        3 or 23 => $"{number}rd",
        _ => $"{number}th",
    };
}
