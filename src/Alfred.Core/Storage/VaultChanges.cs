namespace Alfred.Core.Storage;

internal sealed class TableChanges<TRow>
    where TRow : struct, IVaultRow
{
    internal Dictionary<Guid, TRow> Upserts { get; } = [];

    internal HashSet<Guid> Removals { get; } = [];

    internal bool IsEmpty => Upserts.Count == 0 && Removals.Count == 0;

    internal void Upsert(TRow row)
    {
        Upserts[row.Id] = row;
        _ = Removals.Remove(row.Id);
    }

    internal void Remove(Guid id)
    {
        _ = Upserts.Remove(id);
        _ = Removals.Add(id);
    }

    internal void DrainInto(TableChanges<TRow> target)
    {
        foreach (Guid id in Removals)
        {
            target.Remove(id);
        }

        foreach (KeyValuePair<Guid, TRow> pair in Upserts)
        {
            target.Upsert(pair.Value);
        }

        Clear();
    }

    internal void Clear()
    {
        Upserts.Clear();
        Removals.Clear();
    }
}

internal sealed class VaultChanges
{
    internal TableChanges<LedgerEntryRow> Entries { get; } = new();

    internal TableChanges<TodoRow> Todos { get; } = new();

    internal TableChanges<ReminderRow> Reminders { get; } = new();

    internal TableChanges<PlanRow> Plans { get; } = new();

    internal TableChanges<MealRow> Meals { get; } = new();

    internal TableChanges<WishItemRow> Wishes { get; } = new();

    internal TableChanges<BoardCardRow> Cards { get; } = new();

    internal TableChanges<TrashEntryRow> Trash { get; } = new();

    internal bool IsEmpty =>
        Entries.IsEmpty
        && Todos.IsEmpty
        && Reminders.IsEmpty
        && Plans.IsEmpty
        && Meals.IsEmpty
        && Wishes.IsEmpty
        && Cards.IsEmpty
        && Trash.IsEmpty;

    internal void DrainInto(VaultChanges target)
    {
        Entries.DrainInto(target.Entries);
        Todos.DrainInto(target.Todos);
        Reminders.DrainInto(target.Reminders);
        Plans.DrainInto(target.Plans);
        Meals.DrainInto(target.Meals);
        Wishes.DrainInto(target.Wishes);
        Cards.DrainInto(target.Cards);
        Trash.DrainInto(target.Trash);
    }

    internal void Clear()
    {
        Entries.Clear();
        Todos.Clear();
        Reminders.Clear();
        Plans.Clear();
        Meals.Clear();
        Wishes.Clear();
        Cards.Clear();
        Trash.Clear();
    }
}
