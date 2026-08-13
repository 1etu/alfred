using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Microsoft.Data.Sqlite;

namespace Alfred.Core.Storage;

public sealed class Vault : IDisposable
{
    private const int DebounceMilliseconds = 250;

    private readonly object _gate = new();
    private readonly object _flushGate = new();
    private readonly VaultSnapshot _snapshot = new();
    private readonly VaultChanges _incoming = new();
    private readonly VaultChanges _draining = new();
    private readonly VaultStore _store;
    private readonly Timer _timer;
    private readonly EventHandler _flushOnProcessExit;

    private bool _flushScheduled;
    private volatile bool _disposed;

    public Vault(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        string databasePath = Path.ChangeExtension(path, ".db");
        _store = VaultStore.Open(databasePath);
        _timer = new Timer(FlushDue, null, Timeout.Infinite, Timeout.Infinite);
        Data = Restore(databasePath);

        _flushOnProcessExit = (_, _) => Dispose();
        AppDomain.CurrentDomain.ProcessExit += _flushOnProcessExit;
    }

    public VaultData Data { get; }

    public event EventHandler? Changed;

    public void Save()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            _snapshot.CollectChanges(Data, _incoming);
        }

        ScheduleFlush();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (_flushGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        AppDomain.CurrentDomain.ProcessExit -= _flushOnProcessExit;
        _timer.Dispose();

        lock (_flushGate)
        {
            DrainAndWrite();
            _store.Dispose();
        }
    }

    internal void Flush()
    {
        lock (_flushGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            DrainAndWrite();
        }
    }

    internal void Upsert(LedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _incoming.Entries.Upsert(
                Track(LedgerEntryRow.From(entry, Locate(Data.Entries, entry)), _snapshot.Entries));
        }

        ScheduleFlush();
    }

    internal void Upsert(Todo todo)
    {
        ArgumentNullException.ThrowIfNull(todo);

        lock (_gate)
        {
            _incoming.Todos.Upsert(Track(TodoRow.From(todo, Locate(Data.Todos, todo)), _snapshot.Todos));
        }

        ScheduleFlush();
    }

    internal void Upsert(Reminder reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        lock (_gate)
        {
            _incoming.Reminders.Upsert(
                Track(ReminderRow.From(reminder, Locate(Data.Reminders, reminder)), _snapshot.Reminders));
        }

        ScheduleFlush();
    }

    internal void Upsert(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        lock (_gate)
        {
            _incoming.Plans.Upsert(Track(PlanRow.From(plan, Locate(Data.Plans, plan)), _snapshot.Plans));
        }

        ScheduleFlush();
    }

    internal void Upsert(Meal meal)
    {
        ArgumentNullException.ThrowIfNull(meal);

        lock (_gate)
        {
            _incoming.Meals.Upsert(Track(MealRow.From(meal, Locate(Data.Meals, meal)), _snapshot.Meals));
        }

        ScheduleFlush();
    }

    internal void Upsert(WishItem wish)
    {
        ArgumentNullException.ThrowIfNull(wish);

        lock (_gate)
        {
            _incoming.Wishes.Upsert(Track(WishItemRow.From(wish, Locate(Data.Wishes, wish)), _snapshot.Wishes));
        }

        ScheduleFlush();
    }

    internal void Upsert(BoardCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        lock (_gate)
        {
            _incoming.Cards.Upsert(Track(BoardCardRow.From(card, Locate(Data.Cards, card)), _snapshot.Cards));
        }

        ScheduleFlush();
    }

    internal void Upsert(TrashEntry trashed)
    {
        ArgumentNullException.ThrowIfNull(trashed);

        lock (_gate)
        {
            _incoming.Trash.Upsert(
                Track(TrashEntryRow.From(trashed, Locate(Data.Trash, trashed)), _snapshot.Trash));
        }

        ScheduleFlush();
    }

    internal void Delete(LedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _ = Data.Entries.Remove(entry);
            _ = _snapshot.Entries.Remove(entry.Id);
            _incoming.Entries.Remove(entry.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(Todo todo)
    {
        ArgumentNullException.ThrowIfNull(todo);

        lock (_gate)
        {
            _ = Data.Todos.Remove(todo);
            _ = _snapshot.Todos.Remove(todo.Id);
            _incoming.Todos.Remove(todo.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(Reminder reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        lock (_gate)
        {
            _ = Data.Reminders.Remove(reminder);
            _ = _snapshot.Reminders.Remove(reminder.Id);
            _incoming.Reminders.Remove(reminder.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        lock (_gate)
        {
            _ = Data.Plans.Remove(plan);
            _ = _snapshot.Plans.Remove(plan.Id);
            _incoming.Plans.Remove(plan.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(Meal meal)
    {
        ArgumentNullException.ThrowIfNull(meal);

        lock (_gate)
        {
            _ = Data.Meals.Remove(meal);
            _ = _snapshot.Meals.Remove(meal.Id);
            _incoming.Meals.Remove(meal.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(WishItem wish)
    {
        ArgumentNullException.ThrowIfNull(wish);

        lock (_gate)
        {
            _ = Data.Wishes.Remove(wish);
            _ = _snapshot.Wishes.Remove(wish.Id);
            _incoming.Wishes.Remove(wish.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(BoardCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        lock (_gate)
        {
            _ = Data.Cards.Remove(card);
            _ = _snapshot.Cards.Remove(card.Id);
            _incoming.Cards.Remove(card.Id);
        }

        ScheduleFlush();
    }

    internal void Delete(TrashEntry trashed)
    {
        ArgumentNullException.ThrowIfNull(trashed);

        lock (_gate)
        {
            _ = Data.Trash.Remove(trashed);
            _ = _snapshot.Trash.Remove(trashed.Id);
            _incoming.Trash.Remove(trashed.Id);
        }

        ScheduleFlush();
    }

    private static TRow Track<TRow>(TRow row, Dictionary<Guid, TRow> known)
        where TRow : struct, IVaultRow
    {
        known[row.Id] = row;
        return row;
    }

    private static int Locate<TEntity>(List<TEntity> items, TEntity item)
        where TEntity : class
    {
        int ordinal = items.IndexOf(item);

        if (ordinal >= 0)
        {
            return ordinal;
        }

        items.Add(item);
        return items.Count - 1;
    }

    private static bool IsEmpty(VaultData data) =>
        data.Entries.Count == 0
        && data.Todos.Count == 0
        && data.Reminders.Count == 0
        && data.Plans.Count == 0
        && data.Meals.Count == 0
        && data.Wishes.Count == 0
        && data.Cards.Count == 0
        && data.Trash.Count == 0;

    private VaultData Restore(string databasePath)
    {
        VaultData stored = new();
        _store.LoadInto(stored, _snapshot);

        string legacyPath = LegacyVault.PathBesides(databasePath);

        if (!IsEmpty(stored) || !File.Exists(legacyPath))
        {
            return stored;
        }

        VaultData? imported = LegacyVault.Read(legacyPath);

        if (imported is null)
        {
            return stored;
        }

        _snapshot.CollectChanges(imported, _draining);
        _store.Write(_draining);
        _draining.Clear();
        LegacyVault.MarkImported(legacyPath);

        return imported;
    }

    private void ScheduleFlush()
    {
        bool arm;

        lock (_gate)
        {
            arm = !_flushScheduled && !_incoming.IsEmpty;
            _flushScheduled |= arm;
        }

        if (arm)
        {
            _ = _timer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    private void FlushDue(object? state)
    {
        lock (_flushGate)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                DrainAndWrite();
            }
            catch (SqliteException)
            {
                RetryLater();
            }
        }
    }

    private void RetryLater()
    {
        lock (_gate)
        {
            _flushScheduled = true;
        }

        _ = _timer.Change(DebounceMilliseconds, Timeout.Infinite);
    }

    private void DrainAndWrite()
    {
        lock (_gate)
        {
            _flushScheduled = false;
            _incoming.DrainInto(_draining);
        }

        if (_draining.IsEmpty)
        {
            return;
        }

        _store.Write(_draining);
        _draining.Clear();
    }
}
