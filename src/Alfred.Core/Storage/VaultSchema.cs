using Microsoft.Data.Sqlite;

namespace Alfred.Core.Storage;

internal static class VaultSchema
{
    private const string CreateTables = """
        CREATE TABLE IF NOT EXISTS Entries (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Amount TEXT NOT NULL,
            Currency TEXT NOT NULL,
            Kind INTEGER NOT NULL,
            Cadence INTEGER NOT NULL,
            Anchor TEXT NOT NULL,
            CategoryId TEXT,
            BrandSlug TEXT,
            TagsJson TEXT NOT NULL,
            Notes TEXT,
            SettledJson TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Todos (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Due TEXT,
            Done INTEGER NOT NULL,
            Notes TEXT
        );

        CREATE TABLE IF NOT EXISTS Reminders (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Due TEXT NOT NULL,
            At TEXT,
            Done INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Plans (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Target TEXT,
            StepsJson TEXT NOT NULL,
            Notes TEXT,
            Done INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Meals (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Day TEXT NOT NULL,
            Slot INTEGER NOT NULL,
            Eaten INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Wishes (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Amount TEXT,
            Currency TEXT,
            BrandSlug TEXT,
            Link TEXT,
            Acquired INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Cards (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            "Column" INTEGER NOT NULL,
            Tint INTEGER NOT NULL,
            "Order" INTEGER NOT NULL
        );

        PRAGMA user_version = 1;
        """;

    private const string CreateTrash = """
        CREATE TABLE IF NOT EXISTS Trash (
            Id TEXT NOT NULL PRIMARY KEY,
            Ordinal INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Kind INTEGER NOT NULL,
            Payload TEXT NOT NULL,
            DeletedUtc TEXT NOT NULL
        );

        PRAGMA user_version = 2;
        """;

    private static readonly string[] Migrations = [CreateTables, CreateTrash];

    internal static void Apply(SqliteConnection connection)
    {
        int version = ReadVersion(connection);

        if (version >= Migrations.Length)
        {
            return;
        }

        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        for (int step = version; step < Migrations.Length; step++)
        {
            command.CommandText = Migrations[step];
            _ = command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static int ReadVersion(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return command.ExecuteScalar() is long version ? (int)version : 0;
    }
}
