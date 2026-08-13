using Microsoft.Data.Sqlite;

namespace Alfred.Core.Storage;

internal interface IVaultRow<TSelf, TEntity>
    where TSelf : struct, IVaultRow<TSelf, TEntity>
{
    static abstract TSelf Read(SqliteDataReader reader);

    static abstract TSelf From(TEntity entity, int ordinal);

    Guid Id { get; }

    void Bind(SqliteCommand command);

    TEntity ToEntity();
}

internal static class VaultStatement
{
    internal static SqliteCommand Prepare(SqliteCommand command, params string[] parameterNames)
    {
        foreach (string name in parameterNames)
        {
            _ = command.Parameters.Add(new SqliteParameter(name, DBNull.Value));
        }

        command.Prepare();
        return command;
    }

    internal static void BindId(SqliteCommand command, Guid id) =>
        command.Parameters["$Id"].Value = id.ToString();
}
