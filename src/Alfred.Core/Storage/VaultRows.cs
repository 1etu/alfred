using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Microsoft.Data.Sqlite;

namespace Alfred.Core.Storage;

internal readonly record struct LedgerEntryRow(
    Guid Id,
    int Ordinal,
    string Title,
    string Amount,
    string Currency,
    int Kind,
    int Cadence,
    string Anchor,
    string? CategoryId,
    string? BrandSlug,
    string TagsJson,
    string? Notes,
    string SettledJson) : IVaultRow<LedgerEntryRow, LedgerEntry>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Amount, Currency, Kind, Cadence, Anchor, CategoryId, BrandSlug, TagsJson, Notes, SettledJson
        FROM Entries
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Entries (Id, Ordinal, Title, Amount, Currency, Kind, Cadence, Anchor, CategoryId, BrandSlug, TagsJson, Notes, SettledJson)
        VALUES ($Id, $Ordinal, $Title, $Amount, $Currency, $Kind, $Cadence, $Anchor, $CategoryId, $BrandSlug, $TagsJson, $Notes, $SettledJson);
        """;

    private const string DeleteSql = "DELETE FROM Entries WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(
            command,
            "$Id",
            "$Ordinal",
            "$Title",
            "$Amount",
            "$Currency",
            "$Kind",
            "$Cadence",
            "$Anchor",
            "$CategoryId",
            "$BrandSlug",
            "$TagsJson",
            "$Notes",
            "$SettledJson");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static LedgerEntryRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetInt32(5),
        reader.GetInt32(6),
        reader.GetString(7),
        reader.IsDBNull(8) ? null : reader.GetString(8),
        reader.IsDBNull(9) ? null : reader.GetString(9),
        reader.GetString(10),
        reader.IsDBNull(11) ? null : reader.GetString(11),
        reader.GetString(12));

    public static LedgerEntryRow From(LedgerEntry entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        VaultText.Encode(entity.Money.Amount),
        entity.Money.Currency,
        (int)entity.Kind,
        (int)entity.Schedule.Cadence,
        VaultText.Encode(entity.Schedule.Anchor),
        entity.CategoryId,
        entity.BrandSlug,
        VaultText.EncodeList(entity.Tags),
        entity.Notes,
        VaultText.EncodeList(entity.Settled));

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Amount"].Value = Amount;
        parameters["$Currency"].Value = Currency;
        parameters["$Kind"].Value = Kind;
        parameters["$Cadence"].Value = Cadence;
        parameters["$Anchor"].Value = Anchor;
        parameters["$CategoryId"].Value = (object?)CategoryId ?? DBNull.Value;
        parameters["$BrandSlug"].Value = (object?)BrandSlug ?? DBNull.Value;
        parameters["$TagsJson"].Value = TagsJson;
        parameters["$Notes"].Value = (object?)Notes ?? DBNull.Value;
        parameters["$SettledJson"].Value = SettledJson;
    }

    public LedgerEntry ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Money = new Money(VaultText.DecodeAmount(Amount), Currency),
        Kind = (EntryKind)Kind,
        Schedule = RestoreSchedule(Cadence, Anchor),
        CategoryId = CategoryId,
        BrandSlug = BrandSlug,
        Tags = VaultText.DecodeList<string>(TagsJson),
        Notes = Notes,
        Settled = VaultText.DecodeList<DateOnly>(SettledJson),
    };

    private static Schedule RestoreSchedule(int cadence, string anchor)
    {
        DateOnly anchorDate = VaultText.DecodeDate(anchor);

        return cadence == (int)Ledger.Cadence.None
            ? Schedule.Once(anchorDate)
            : Schedule.Every((Ledger.Cadence)cadence, anchorDate);
    }
}

internal readonly record struct TodoRow(
    Guid Id,
    int Ordinal,
    string Title,
    string? Due,
    bool Done,
    string? Notes) : IVaultRow<TodoRow, Todo>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Due, Done, Notes
        FROM Todos
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Todos (Id, Ordinal, Title, Due, Done, Notes)
        VALUES ($Id, $Ordinal, $Title, $Due, $Done, $Notes);
        """;

    private const string DeleteSql = "DELETE FROM Todos WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Due", "$Done", "$Notes");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static TodoRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetBoolean(4),
        reader.IsDBNull(5) ? null : reader.GetString(5));

    public static TodoRow From(Todo entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        VaultText.EncodeOptional(entity.Due),
        entity.Done,
        entity.Notes);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Due"].Value = (object?)Due ?? DBNull.Value;
        parameters["$Done"].Value = Done;
        parameters["$Notes"].Value = (object?)Notes ?? DBNull.Value;
    }

    public Todo ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Due = VaultText.DecodeOptionalDate(Due),
        Done = Done,
        Notes = Notes,
    };
}

internal readonly record struct ReminderRow(
    Guid Id,
    int Ordinal,
    string Title,
    string Due,
    string? At,
    bool Done) : IVaultRow<ReminderRow, Reminder>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Due, At, Done
        FROM Reminders
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Reminders (Id, Ordinal, Title, Due, At, Done)
        VALUES ($Id, $Ordinal, $Title, $Due, $At, $Done);
        """;

    private const string DeleteSql = "DELETE FROM Reminders WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Due", "$At", "$Done");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static ReminderRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetBoolean(5));

    public static ReminderRow From(Reminder entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        VaultText.Encode(entity.Due),
        VaultText.EncodeOptional(entity.At),
        entity.Done);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Due"].Value = Due;
        parameters["$At"].Value = (object?)At ?? DBNull.Value;
        parameters["$Done"].Value = Done;
    }

    public Reminder ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Due = VaultText.DecodeDate(Due),
        At = VaultText.DecodeOptionalTime(At),
        Done = Done,
    };
}

internal readonly record struct PlanRow(
    Guid Id,
    int Ordinal,
    string Title,
    string? Target,
    string StepsJson,
    string? Notes,
    bool Done) : IVaultRow<PlanRow, Plan>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Target, StepsJson, Notes, Done
        FROM Plans
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Plans (Id, Ordinal, Title, Target, StepsJson, Notes, Done)
        VALUES ($Id, $Ordinal, $Title, $Target, $StepsJson, $Notes, $Done);
        """;

    private const string DeleteSql = "DELETE FROM Plans WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Target", "$StepsJson", "$Notes", "$Done");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static PlanRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.GetBoolean(6));

    public static PlanRow From(Plan entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        VaultText.EncodeOptional(entity.Target),
        VaultText.EncodeList(entity.Steps),
        entity.Notes,
        entity.Done);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Target"].Value = (object?)Target ?? DBNull.Value;
        parameters["$StepsJson"].Value = StepsJson;
        parameters["$Notes"].Value = (object?)Notes ?? DBNull.Value;
        parameters["$Done"].Value = Done;
    }

    public Plan ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Target = VaultText.DecodeOptionalDate(Target),
        Steps = VaultText.DecodeList<PlanStep>(StepsJson),
        Notes = Notes,
        Done = Done,
    };
}

internal readonly record struct MealRow(
    Guid Id,
    int Ordinal,
    string Title,
    string Day,
    int Slot,
    bool Eaten) : IVaultRow<MealRow, Meal>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Day, Slot, Eaten
        FROM Meals
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Meals (Id, Ordinal, Title, Day, Slot, Eaten)
        VALUES ($Id, $Ordinal, $Title, $Day, $Slot, $Eaten);
        """;

    private const string DeleteSql = "DELETE FROM Meals WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Day", "$Slot", "$Eaten");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static MealRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetInt32(4),
        reader.GetBoolean(5));

    public static MealRow From(Meal entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        VaultText.Encode(entity.Day),
        (int)entity.Slot,
        entity.Eaten);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Day"].Value = Day;
        parameters["$Slot"].Value = Slot;
        parameters["$Eaten"].Value = Eaten;
    }

    public Meal ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Day = VaultText.DecodeDate(Day),
        Slot = (MealSlot)Slot,
        Eaten = Eaten,
    };
}

internal readonly record struct WishItemRow(
    Guid Id,
    int Ordinal,
    string Title,
    string? Amount,
    string? Currency,
    string? BrandSlug,
    string? Link,
    bool Acquired) : IVaultRow<WishItemRow, WishItem>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Amount, Currency, BrandSlug, Link, Acquired
        FROM Wishes
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Wishes (Id, Ordinal, Title, Amount, Currency, BrandSlug, Link, Acquired)
        VALUES ($Id, $Ordinal, $Title, $Amount, $Currency, $BrandSlug, $Link, $Acquired);
        """;

    private const string DeleteSql = "DELETE FROM Wishes WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(
            command,
            "$Id",
            "$Ordinal",
            "$Title",
            "$Amount",
            "$Currency",
            "$BrandSlug",
            "$Link",
            "$Acquired");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static WishItemRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.GetBoolean(7));

    public static WishItemRow From(WishItem entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        entity.Price is null ? null : VaultText.Encode(entity.Price.Value.Amount),
        entity.Price?.Currency,
        entity.BrandSlug,
        entity.Link,
        entity.Acquired);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Amount"].Value = (object?)Amount ?? DBNull.Value;
        parameters["$Currency"].Value = (object?)Currency ?? DBNull.Value;
        parameters["$BrandSlug"].Value = (object?)BrandSlug ?? DBNull.Value;
        parameters["$Link"].Value = (object?)Link ?? DBNull.Value;
        parameters["$Acquired"].Value = Acquired;
    }

    public WishItem ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Price = RestorePrice(Amount, Currency),
        BrandSlug = BrandSlug,
        Link = Link,
        Acquired = Acquired,
    };

    private static Money? RestorePrice(string? amount, string? currency)
    {
        if (amount is null || currency is null)
        {
            return null;
        }

        return new Money(VaultText.DecodeAmount(amount), currency);
    }
}

internal readonly record struct TrashEntryRow(
    Guid Id,
    int Ordinal,
    string Title,
    int Kind,
    string Payload,
    string DeletedUtc) : IVaultRow<TrashEntryRow, TrashEntry>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, Kind, Payload, DeletedUtc
        FROM Trash
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Trash (Id, Ordinal, Title, Kind, Payload, DeletedUtc)
        VALUES ($Id, $Ordinal, $Title, $Kind, $Payload, $DeletedUtc);
        """;

    private const string DeleteSql = "DELETE FROM Trash WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Kind", "$Payload", "$DeletedUtc");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static TrashEntryRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.GetString(4),
        reader.GetString(5));

    public static TrashEntryRow From(TrashEntry entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        (int)entity.Kind,
        entity.Payload,
        VaultText.Encode(entity.DeletedUtc));

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Kind"].Value = Kind;
        parameters["$Payload"].Value = Payload;
        parameters["$DeletedUtc"].Value = DeletedUtc;
    }

    public TrashEntry ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Kind = (TrashKind)Kind,
        Payload = Payload,
        DeletedUtc = VaultText.DecodeInstant(DeletedUtc),
    };
}

internal readonly record struct BoardCardRow(
    Guid Id,
    int Ordinal,
    string Title,
    int Column,
    int Tint,
    int Order) : IVaultRow<BoardCardRow, BoardCard>
{
    internal const string SelectSql = """
        SELECT Id, Ordinal, Title, "Column", Tint, "Order"
        FROM Cards
        ORDER BY Ordinal;
        """;

    private const string UpsertSql = """
        INSERT OR REPLACE INTO Cards (Id, Ordinal, Title, "Column", Tint, "Order")
        VALUES ($Id, $Ordinal, $Title, $Column, $Tint, $Order);
        """;

    private const string DeleteSql = "DELETE FROM Cards WHERE Id = $Id;";

    internal static SqliteCommand CreateUpsert(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpsertSql;
        return VaultStatement.Prepare(command, "$Id", "$Ordinal", "$Title", "$Column", "$Tint", "$Order");
    }

    internal static SqliteCommand CreateDelete(SqliteConnection connection)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        return VaultStatement.Prepare(command, "$Id");
    }

    public static BoardCardRow Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.GetInt32(4),
        reader.GetInt32(5));

    public static BoardCardRow From(BoardCard entity, int ordinal) => new(
        entity.Id,
        ordinal,
        entity.Title,
        (int)entity.Column,
        entity.Tint,
        entity.Order);

    public void Bind(SqliteCommand command)
    {
        SqliteParameterCollection parameters = command.Parameters;
        parameters["$Id"].Value = Id.ToString();
        parameters["$Ordinal"].Value = Ordinal;
        parameters["$Title"].Value = Title;
        parameters["$Column"].Value = Column;
        parameters["$Tint"].Value = Tint;
        parameters["$Order"].Value = Order;
    }

    public BoardCard ToEntity() => new()
    {
        Id = Id,
        Title = Title,
        Column = (BoardColumn)Column,
        Tint = Tint,
        Order = Order,
    };
}
