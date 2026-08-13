using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Xunit;

namespace Alfred.Core.Tests.Storage;

public sealed class RecyclerTests
{
    [Fact]
    public void DeleteMovesToTrashAndRestoreBringsItBack()
    {
        VaultData data = new();
        Todo todo = new() { Title = "Call the dentist", Due = new DateOnly(2026, 8, 20) };
        data.Todos.Add(todo);

        Recycler.Delete(data, todo);

        Assert.Empty(data.Todos);
        Assert.Single(data.Trash);
        Assert.Equal(TrashKind.Todo, data.Trash[0].Kind);

        Assert.True(Recycler.Restore(data, data.Trash[0]));
        Assert.Single(data.Todos);
        Assert.Empty(data.Trash);
        Assert.Equal("Call the dentist", data.Todos[0].Title);
        Assert.Equal(new DateOnly(2026, 8, 20), data.Todos[0].Due);
    }

    [Fact]
    public void LedgerEntryRoundTripsThroughTrash()
    {
        VaultData data = new();
        LedgerEntry entry = new()
        {
            Title = "Netflix",
            Money = Money.Lira(229.99m),
            Kind = EntryKind.Expense,
            Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 13)),
            BrandSlug = "netflix",
        };
        data.Entries.Add(entry);

        Recycler.Delete(data, entry);
        Assert.True(Recycler.Restore(data, data.Trash[0]));

        LedgerEntry restored = data.Entries[0];
        Assert.Equal(229.99m, restored.Money.Amount);
        Assert.Equal(Cadence.Monthly, restored.Schedule.Cadence);
        Assert.Equal("netflix", restored.BrandSlug);
    }

    [Fact]
    public void PurgeRemovesOnlyExpiredEntries()
    {
        VaultData data = new();
        DateTimeOffset now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

        data.Trash.Add(new TrashEntry { Kind = TrashKind.Todo, Title = "old", Payload = "{}", DeletedUtc = now.AddDays(-31) });
        data.Trash.Add(new TrashEntry { Kind = TrashKind.Todo, Title = "edge", Payload = "{}", DeletedUtc = now.AddDays(-30) });
        data.Trash.Add(new TrashEntry { Kind = TrashKind.Todo, Title = "fresh", Payload = "{}", DeletedUtc = now.AddDays(-2) });

        int removed = Recycler.Purge(data, now);

        Assert.Equal(2, removed);
        Assert.Single(data.Trash);
        Assert.Equal("fresh", data.Trash[0].Title);
    }

    [Fact]
    public void DaysLeftCountsDown()
    {
        DateTimeOffset now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        TrashEntry entry = new() { Kind = TrashKind.Todo, Title = "x", Payload = "{}", DeletedUtc = now.AddDays(-4) };

        Assert.Equal(26, entry.DaysLeft(now));
    }

    [Fact]
    public void RestoreFailsGracefullyOnCorruptPayload()
    {
        VaultData data = new();
        TrashEntry entry = new() { Kind = TrashKind.Todo, Title = "x", Payload = "{not json", DeletedUtc = DateTimeOffset.UtcNow };
        data.Trash.Add(entry);

        Assert.False(Recycler.Restore(data, entry));
        Assert.Single(data.Trash);
    }
}
