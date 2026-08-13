using Alfred.Core.Ledger;
using Alfred.Core.Storage;

namespace Alfred.Core.Agenda;

public sealed record DayMoney(decimal Out, decimal In);

public static class AgendaService
{
    public static IReadOnlyList<AgendaItem> Today(VaultData data, DateOnly today)
    {
        List<AgendaItem> items = [.. Overdue(data, today), .. OnDay(data, today, today)];
        return items;
    }

    public static IReadOnlyList<AgendaItem> Upcoming(VaultData data, DateOnly today, int horizonDays)
    {
        List<AgendaItem> items = [];

        for (DateOnly day = today.AddDays(1); day <= today.AddDays(horizonDays); day = day.AddDays(1))
        {
            items.AddRange(OnDay(data, day, today));
        }

        return items;
    }

    public static IReadOnlyList<AgendaItem> On(VaultData data, DateOnly day) =>
        [.. OnDay(data, day, day)];

    public static DayMoney MoneyOn(VaultData data, DateOnly day)
    {
        decimal moneyOut = 0;
        decimal moneyIn = 0;

        foreach (LedgerEntry entry in data.Entries)
        {
            foreach (DateOnly _ in entry.Schedule.Occurrences(day, day))
            {
                if (entry.Flow == CashFlow.In)
                {
                    moneyIn += entry.Money.Amount;
                }
                else
                {
                    moneyOut += entry.Money.Amount;
                }
            }
        }

        return new DayMoney(moneyOut, moneyIn);
    }

    private static IEnumerable<AgendaItem> Overdue(VaultData data, DateOnly today)
    {
        foreach (var todo in data.Todos.Where(todo => !todo.Done && todo.Due is { } due && due < today))
        {
            yield return new AgendaItem(todo.Id, todo.Due!.Value, AgendaKind.Do, todo.Title, "TODOs", null, null, null, false, true);
        }

        foreach (var reminder in data.Reminders.Where(reminder => !reminder.Done && reminder.Due < today))
        {
            yield return new AgendaItem(reminder.Id, reminder.Due, AgendaKind.Do, reminder.Title, "Reminders", null, null, null, false, true);
        }

        foreach (LedgerEntry entry in data.Entries.Where(entry => entry.Kind == EntryKind.Payment))
        {
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(today.AddDays(-60), today.AddDays(-1)))
            {
                if (!entry.IsSettled(occurrence))
                {
                    yield return new AgendaItem(entry.Id, occurrence, AgendaKind.Settle, entry.Title, "Payments", entry.Money, entry.Flow, entry.BrandSlug, false, true);
                }
            }
        }
    }

    private static IEnumerable<AgendaItem> OnDay(VaultData data, DateOnly day, DateOnly today)
    {
        foreach (var todo in data.Todos.Where(todo => todo.Due == day && (day > today || !todo.Done || day == today)))
        {
            if (todo.Done && day != today)
            {
                continue;
            }

            yield return new AgendaItem(todo.Id, day, AgendaKind.Do, todo.Title, "TODOs", null, null, null, todo.Done, false);
        }

        foreach (var reminder in data.Reminders.Where(reminder => reminder.Due == day))
        {
            if (reminder.Done && day != today)
            {
                continue;
            }

            string title = reminder.At is { } at ? $"{reminder.Title}, {at:HH\\:mm}" : reminder.Title;
            yield return new AgendaItem(reminder.Id, day, AgendaKind.Do, title, "Reminders", null, null, null, reminder.Done, false);
        }

        foreach (var plan in data.Plans.Where(plan => !plan.Done && plan.Target == day))
        {
            yield return new AgendaItem(plan.Id, day, AgendaKind.Do, plan.Title, "Plans", null, null, null, false, false);
        }

        foreach (LedgerEntry entry in data.Entries)
        {
            foreach (DateOnly _ in entry.Schedule.Occurrences(day, day))
            {
                AgendaKind kind = entry.Kind == EntryKind.Payment ? AgendaKind.Settle : AgendaKind.Know;
                bool done = kind == AgendaKind.Settle && entry.IsSettled(day);
                yield return new AgendaItem(entry.Id, day, kind, entry.Title, "Payments", entry.Money, entry.Flow, entry.BrandSlug, done, false);
            }
        }

        foreach (var meal in data.Meals.Where(meal => meal.Day == day))
        {
            yield return new AgendaItem(meal.Id, day, AgendaKind.Know, meal.Title, "Meals", null, null, null, meal.Eaten, false);
        }
    }
}
