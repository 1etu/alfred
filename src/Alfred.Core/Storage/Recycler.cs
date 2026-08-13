using System.Text.Json;
using Alfred.Core.Items;
using Alfred.Core.Ledger;

namespace Alfred.Core.Storage;

public static class Recycler
{
    public static void Delete(VaultData data, LedgerEntry entry) =>
        Move(data, TrashKind.LedgerEntry, entry.Title, entry, () => data.Entries.Remove(entry));

    public static void Delete(VaultData data, Todo todo) =>
        Move(data, TrashKind.Todo, todo.Title, todo, () => data.Todos.Remove(todo));

    public static void Delete(VaultData data, Reminder reminder) =>
        Move(data, TrashKind.Reminder, reminder.Title, reminder, () => data.Reminders.Remove(reminder));

    public static void Delete(VaultData data, Plan plan) =>
        Move(data, TrashKind.Plan, plan.Title, plan, () => data.Plans.Remove(plan));

    public static void Delete(VaultData data, Meal meal) =>
        Move(data, TrashKind.Meal, meal.Title, meal, () => data.Meals.Remove(meal));

    public static void Delete(VaultData data, WishItem wish) =>
        Move(data, TrashKind.Wish, wish.Title, wish, () => data.Wishes.Remove(wish));

    public static void Delete(VaultData data, BoardCard card) =>
        Move(data, TrashKind.Card, card.Title, card, () => data.Cards.Remove(card));

    public static bool Restore(VaultData data, TrashEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            switch (entry.Kind)
            {
                case TrashKind.LedgerEntry:
                    data.Entries.Add(Read<LedgerEntry>(entry));
                    break;
                case TrashKind.Todo:
                    data.Todos.Add(Read<Todo>(entry));
                    break;
                case TrashKind.Reminder:
                    data.Reminders.Add(Read<Reminder>(entry));
                    break;
                case TrashKind.Plan:
                    data.Plans.Add(Read<Plan>(entry));
                    break;
                case TrashKind.Meal:
                    data.Meals.Add(Read<Meal>(entry));
                    break;
                case TrashKind.Wish:
                    data.Wishes.Add(Read<WishItem>(entry));
                    break;
                case TrashKind.Card:
                    data.Cards.Add(Read<BoardCard>(entry));
                    break;
                default:
                    return false;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        data.Trash.Remove(entry);
        return true;
    }

    public static int Purge(VaultData data, DateTimeOffset now)
    {
        int removed = data.Trash.RemoveAll(entry => TrashPolicy.IsExpired(entry, now));
        return removed;
    }

    private static void Move<T>(VaultData data, TrashKind kind, string title, T item, Action remove)
    {
        data.Trash.Add(new TrashEntry
        {
            Kind = kind,
            Title = title,
            Payload = JsonSerializer.Serialize(item),
            DeletedUtc = DateTimeOffset.UtcNow,
        });

        remove();
    }

    private static T Read<T>(TrashEntry entry) =>
        JsonSerializer.Deserialize<T>(entry.Payload) ?? throw new JsonException("Empty payload.");
}
