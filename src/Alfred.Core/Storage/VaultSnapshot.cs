namespace Alfred.Core.Storage;

internal sealed class VaultSnapshot
{
    private readonly HashSet<Guid> _present = [];

    private readonly List<Guid> _dropped = [];

    internal Dictionary<Guid, LedgerEntryRow> Entries { get; } = [];

    internal Dictionary<Guid, TodoRow> Todos { get; } = [];

    internal Dictionary<Guid, ReminderRow> Reminders { get; } = [];

    internal Dictionary<Guid, PlanRow> Plans { get; } = [];

    internal Dictionary<Guid, MealRow> Meals { get; } = [];

    internal Dictionary<Guid, WishItemRow> Wishes { get; } = [];

    internal Dictionary<Guid, BoardCardRow> Cards { get; } = [];

    internal Dictionary<Guid, TrashEntryRow> Trash { get; } = [];

    internal void CollectChanges(VaultData data, VaultChanges changes)
    {
        Collect(data.Entries, Entries, changes.Entries);
        Collect(data.Todos, Todos, changes.Todos);
        Collect(data.Reminders, Reminders, changes.Reminders);
        Collect(data.Plans, Plans, changes.Plans);
        Collect(data.Meals, Meals, changes.Meals);
        Collect(data.Wishes, Wishes, changes.Wishes);
        Collect(data.Cards, Cards, changes.Cards);
        Collect(data.Trash, Trash, changes.Trash);
    }

    private void Collect<TRow, TEntity>(
        List<TEntity> items,
        Dictionary<Guid, TRow> known,
        TableChanges<TRow> changes)
        where TRow : struct, IVaultRow<TRow, TEntity>, IEquatable<TRow>
    {
        _present.Clear();

        for (int ordinal = 0; ordinal < items.Count; ordinal++)
        {
            TRow row = TRow.From(items[ordinal], ordinal);
            _ = _present.Add(row.Id);

            if (known.TryGetValue(row.Id, out TRow previous) && previous.Equals(row))
            {
                continue;
            }

            known[row.Id] = row;
            changes.Upsert(row);
        }

        _dropped.Clear();

        foreach (Guid id in known.Keys)
        {
            if (!_present.Contains(id))
            {
                _dropped.Add(id);
            }
        }

        foreach (Guid id in _dropped)
        {
            _ = known.Remove(id);
            changes.Remove(id);
        }
    }
}
