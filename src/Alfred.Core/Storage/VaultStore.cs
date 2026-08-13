using Microsoft.Data.Sqlite;

namespace Alfred.Core.Storage;

internal sealed class VaultStore : IDisposable
{
    private const string Pragmas = """
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = NORMAL;
        PRAGMA temp_store = MEMORY;
        PRAGMA foreign_keys = ON;
        """;

    private readonly SqliteConnection _connection;
    private readonly SqliteCommand _upsertEntry;
    private readonly SqliteCommand _deleteEntry;
    private readonly SqliteCommand _upsertTodo;
    private readonly SqliteCommand _deleteTodo;
    private readonly SqliteCommand _upsertReminder;
    private readonly SqliteCommand _deleteReminder;
    private readonly SqliteCommand _upsertPlan;
    private readonly SqliteCommand _deletePlan;
    private readonly SqliteCommand _upsertMeal;
    private readonly SqliteCommand _deleteMeal;
    private readonly SqliteCommand _upsertWish;
    private readonly SqliteCommand _deleteWish;
    private readonly SqliteCommand _upsertCard;
    private readonly SqliteCommand _deleteCard;
    private readonly SqliteCommand _upsertTrash;
    private readonly SqliteCommand _deleteTrash;
    private readonly SqliteCommand[] _statements;

    private VaultStore(SqliteConnection connection)
    {
        _connection = connection;
        _upsertEntry = LedgerEntryRow.CreateUpsert(connection);
        _deleteEntry = LedgerEntryRow.CreateDelete(connection);
        _upsertTodo = TodoRow.CreateUpsert(connection);
        _deleteTodo = TodoRow.CreateDelete(connection);
        _upsertReminder = ReminderRow.CreateUpsert(connection);
        _deleteReminder = ReminderRow.CreateDelete(connection);
        _upsertPlan = PlanRow.CreateUpsert(connection);
        _deletePlan = PlanRow.CreateDelete(connection);
        _upsertMeal = MealRow.CreateUpsert(connection);
        _deleteMeal = MealRow.CreateDelete(connection);
        _upsertWish = WishItemRow.CreateUpsert(connection);
        _deleteWish = WishItemRow.CreateDelete(connection);
        _upsertCard = BoardCardRow.CreateUpsert(connection);
        _deleteCard = BoardCardRow.CreateDelete(connection);
        _upsertTrash = TrashEntryRow.CreateUpsert(connection);
        _deleteTrash = TrashEntryRow.CreateDelete(connection);

        _statements =
        [
            _upsertEntry, _deleteEntry,
            _upsertTodo, _deleteTodo,
            _upsertReminder, _deleteReminder,
            _upsertPlan, _deletePlan,
            _upsertMeal, _deleteMeal,
            _upsertWish, _deleteWish,
            _upsertCard, _deleteCard,
            _upsertTrash, _deleteTrash,
        ];
    }

    internal static VaultStore Open(string path)
    {
        string? folder = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(folder))
        {
            _ = Directory.CreateDirectory(folder);
        }

        try
        {
            return Connect(path);
        }
        catch (SqliteException)
        {
            Quarantine(path);
            return Connect(path);
        }
    }

    internal void LoadInto(VaultData data, VaultSnapshot snapshot)
    {
        using SqliteCommand command = _connection.CreateCommand();

        command.CommandText = LedgerEntryRow.SelectSql;
        Load(command, data.Entries, snapshot.Entries);

        command.CommandText = TodoRow.SelectSql;
        Load(command, data.Todos, snapshot.Todos);

        command.CommandText = ReminderRow.SelectSql;
        Load(command, data.Reminders, snapshot.Reminders);

        command.CommandText = PlanRow.SelectSql;
        Load(command, data.Plans, snapshot.Plans);

        command.CommandText = MealRow.SelectSql;
        Load(command, data.Meals, snapshot.Meals);

        command.CommandText = WishItemRow.SelectSql;
        Load(command, data.Wishes, snapshot.Wishes);

        command.CommandText = BoardCardRow.SelectSql;
        Load(command, data.Cards, snapshot.Cards);

        command.CommandText = TrashEntryRow.SelectSql;
        Load(command, data.Trash, snapshot.Trash);
    }

    internal void Write(VaultChanges changes)
    {
        using SqliteTransaction transaction = _connection.BeginTransaction();

        foreach (SqliteCommand statement in _statements)
        {
            statement.Transaction = transaction;
        }

        Apply(changes.Entries, _upsertEntry, _deleteEntry);
        Apply(changes.Todos, _upsertTodo, _deleteTodo);
        Apply(changes.Reminders, _upsertReminder, _deleteReminder);
        Apply(changes.Plans, _upsertPlan, _deletePlan);
        Apply(changes.Meals, _upsertMeal, _deleteMeal);
        Apply(changes.Wishes, _upsertWish, _deleteWish);
        Apply(changes.Cards, _upsertCard, _deleteCard);
        Apply(changes.Trash, _upsertTrash, _deleteTrash);

        transaction.Commit();
    }

    public void Dispose()
    {
        foreach (SqliteCommand statement in _statements)
        {
            statement.Dispose();
        }

        _connection.Dispose();
    }

    private static VaultStore Connect(string path)
    {
        SqliteConnectionStringBuilder connectionString = new()
        {
            DataSource = path,
            Pooling = false,
        };

        SqliteConnection connection = new(connectionString.ConnectionString);

        try
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = Pragmas;
                _ = command.ExecuteNonQuery();
            }

            VaultSchema.Apply(connection);
            return new VaultStore(connection);
        }
        catch (SqliteException)
        {
            connection.Dispose();
            throw;
        }
    }

    private static void Quarantine(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        File.Move(path, path + ".corrupt", overwrite: true);
        File.Delete(path + "-wal");
        File.Delete(path + "-shm");
    }

    private static void Load<TRow, TEntity>(SqliteCommand command, List<TEntity> items, Dictionary<Guid, TRow> known)
        where TRow : struct, IVaultRow<TRow, TEntity>
    {
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            TRow row = TRow.Read(reader);
            items.Add(row.ToEntity());
            known[row.Id] = row;
        }
    }

    private static void Apply<TRow>(TableChanges<TRow> changes, SqliteCommand upsert, SqliteCommand delete)
        where TRow : struct, IVaultRow
    {
        foreach (Guid id in changes.Removals)
        {
            VaultStatement.BindId(delete, id);
            _ = delete.ExecuteNonQuery();
        }

        foreach (KeyValuePair<Guid, TRow> pair in changes.Upserts)
        {
            pair.Value.Bind(upsert);
            _ = upsert.ExecuteNonQuery();
        }
    }
}
