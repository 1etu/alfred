using System.Text.Json;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Xunit;

namespace Alfred.Core.Tests.Storage;

public sealed class VaultTests
{
    [Fact]
    public void RoundTripsAllEntityKinds()
    {
        using VaultFolder folder = new();

        using (Vault vault = new(folder.DatabasePath))
        {
            vault.Data.Entries.Add(new LedgerEntry
            {
                Title = "Rent",
                Money = Money.Lira(15000),
                Kind = EntryKind.Payment,
                Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 11)),
                CategoryId = "housing",
                BrandSlug = "landlord",
                Notes = "wire before the 5th",
                Tags = ["fixed", "home"],
                Settled = [new DateOnly(2026, 8, 11), new DateOnly(2026, 7, 11)],
            });
            vault.Data.Todos.Add(new Todo { Title = "Call dentist", Due = new DateOnly(2026, 8, 20), Notes = "ask about x-ray" });
            vault.Data.Reminders.Add(new Reminder { Title = "Standup", Due = new DateOnly(2026, 8, 14), At = new TimeOnly(10, 0) });
            vault.Data.Plans.Add(new Plan
            {
                Title = "Trip",
                Target = new DateOnly(2026, 9, 1),
                Steps = [new PlanStep { Title = "Book" }, new PlanStep { Title = "Pack", Done = true }],
            });
            vault.Data.Meals.Add(new Meal { Title = "Pasta", Day = new DateOnly(2026, 8, 13), Slot = MealSlot.Dinner });
            vault.Data.Wishes.Add(new WishItem { Title = "Keyboard", Price = Money.Lira(4500), Link = "https://example.com" });
            vault.Data.Cards.Add(new BoardCard { Title = "Idea", Column = BoardColumn.Doing, Tint = 2, Order = 3 });
            vault.Data.Trash.Add(new TrashEntry
            {
                Kind = TrashKind.Todo,
                Title = "Old chore",
                Payload = "{}",
                DeletedUtc = new DateTimeOffset(2026, 8, 1, 9, 30, 0, TimeSpan.FromHours(3)),
            });
            vault.Save();
        }

        using Vault reloaded = new(folder.DatabasePath);

        LedgerEntry entry = reloaded.Data.Entries[0];
        Assert.Equal("Rent", entry.Title);
        Assert.Equal(Cadence.Monthly, entry.Schedule.Cadence);
        Assert.Equal(new DateOnly(2026, 8, 11), entry.Schedule.Anchor);
        Assert.Equal("housing", entry.CategoryId);
        Assert.Equal("wire before the 5th", entry.Notes);
        Assert.Equal(["fixed", "home"], entry.Tags);
        Assert.True(entry.IsSettled(new DateOnly(2026, 8, 11)));
        Assert.True(entry.IsSettled(new DateOnly(2026, 7, 11)));

        Assert.Equal(new DateOnly(2026, 8, 20), reloaded.Data.Todos[0].Due);
        Assert.Equal("ask about x-ray", reloaded.Data.Todos[0].Notes);
        Assert.Equal(new TimeOnly(10, 0), reloaded.Data.Reminders[0].At);

        Plan plan = reloaded.Data.Plans[0];
        Assert.Equal(new DateOnly(2026, 9, 1), plan.Target);
        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("Book", plan.Steps[0].Title);
        Assert.True(plan.Steps[1].Done);

        Assert.Equal(MealSlot.Dinner, reloaded.Data.Meals[0].Slot);
        Assert.Equal(4500, reloaded.Data.Wishes[0].Price!.Value.Amount);
        Assert.Equal("https://example.com", reloaded.Data.Wishes[0].Link);
        Assert.Equal(BoardColumn.Doing, reloaded.Data.Cards[0].Column);
        Assert.Equal(3, reloaded.Data.Cards[0].Order);

        TrashEntry trashed = reloaded.Data.Trash[0];
        Assert.Equal(TrashKind.Todo, trashed.Kind);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 9, 30, 0, TimeSpan.FromHours(3)), trashed.DeletedUtc);
    }

    [Fact]
    public void PreservesDecimalPrecision()
    {
        using VaultFolder folder = new();

        using (Vault vault = new(folder.DatabasePath))
        {
            vault.Data.Entries.Add(new LedgerEntry
            {
                Title = "Netflix",
                Money = Money.Lira(229.99m),
                Kind = EntryKind.Expense,
                Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 13)),
            });
            vault.Data.Entries.Add(new LedgerEntry
            {
                Title = "Salary",
                Money = Money.Lira(42000),
                Kind = EntryKind.Income,
                Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 1)),
            });
            vault.Data.Wishes.Add(new WishItem { Title = "Desk", Price = new Money(1234.5678m, "USD") });
            vault.Save();
        }

        using Vault reloaded = new(folder.DatabasePath);

        Assert.Equal(229.99m, reloaded.Data.Entries[0].Money.Amount);
        Assert.Equal(42000m, reloaded.Data.Entries[1].Money.Amount);
        Assert.Equal(1234.5678m, reloaded.Data.Wishes[0].Price!.Value.Amount);
        Assert.Equal("USD", reloaded.Data.Wishes[0].Price!.Value.Currency);
    }

    [Fact]
    public void EditsAndRemovalsSurviveReopen()
    {
        using VaultFolder folder = new();
        Guid keptId;

        using (Vault vault = new(folder.DatabasePath))
        {
            vault.Data.Todos.Add(new Todo { Title = "First" });
            vault.Data.Todos.Add(new Todo { Title = "Second" });
            vault.Data.Todos.Add(new Todo { Title = "Third" });
            vault.Save();

            keptId = vault.Data.Todos[2].Id;
            vault.Data.Todos[2].Done = true;
            vault.Data.Todos.RemoveAt(0);
            vault.Save();
        }

        using Vault reloaded = new(folder.DatabasePath);

        Assert.Equal(2, reloaded.Data.Todos.Count);
        Assert.Equal("Second", reloaded.Data.Todos[0].Title);
        Assert.Equal("Third", reloaded.Data.Todos[1].Title);
        Assert.Equal(keptId, reloaded.Data.Todos[1].Id);
        Assert.True(reloaded.Data.Todos[1].Done);
    }

    [Fact]
    public void MissingFileStartsEmpty()
    {
        using VaultFolder folder = new();

        using Vault vault = new(folder.DatabasePath);

        Assert.Empty(vault.Data.Entries);
        Assert.Empty(vault.Data.Todos);
        Assert.Empty(vault.Data.Trash);
    }

    [Fact]
    public void CorruptDatabaseFallsBackToEmpty()
    {
        using VaultFolder folder = new();
        File.WriteAllText(folder.DatabasePath, "{not a database");

        using (Vault vault = new(folder.DatabasePath))
        {
            Assert.Empty(vault.Data.Entries);

            vault.Data.Todos.Add(new Todo { Title = "Still works" });
            vault.Save();
        }

        Assert.True(File.Exists(folder.DatabasePath + ".corrupt"));

        using Vault reopened = new(folder.DatabasePath);
        Assert.Equal("Still works", reopened.Data.Todos[0].Title);
    }

    [Fact]
    public void ImportsLegacyJsonAndRenamesIt()
    {
        using VaultFolder folder = new();
        WriteLegacyVault(folder.LegacyPath);

        using (Vault vault = new(folder.LegacyPath))
        {
            Assert.Equal("Rent", vault.Data.Entries[0].Title);
            Assert.Equal(15000m, vault.Data.Entries[0].Money.Amount);
            Assert.True(vault.Data.Entries[0].IsSettled(new DateOnly(2026, 8, 11)));
            Assert.Equal("Call dentist", vault.Data.Todos[0].Title);
            Assert.Single(vault.Data.Plans[0].Steps);
        }

        Assert.False(File.Exists(folder.LegacyPath));
        Assert.True(File.Exists(folder.LegacyPath + ".imported"));
        Assert.True(File.Exists(folder.DatabasePath));

        using Vault reopened = new(folder.LegacyPath);
        Assert.Single(reopened.Data.Entries);
        Assert.Single(reopened.Data.Todos);
        Assert.Single(reopened.Data.Plans);
    }

    [Fact]
    public void LegacyImportDoesNotRepeatWhenJsonReappears()
    {
        using VaultFolder folder = new();
        WriteLegacyVault(folder.LegacyPath);

        using (Vault first = new(folder.LegacyPath))
        {
            Assert.Single(first.Data.Entries);
        }

        WriteLegacyVault(folder.LegacyPath);

        using Vault second = new(folder.LegacyPath);

        Assert.Single(second.Data.Entries);
        Assert.True(File.Exists(folder.LegacyPath));
    }

    [Fact]
    public void CorruptLegacyJsonLeavesTheFileAlone()
    {
        using VaultFolder folder = new();
        File.WriteAllText(folder.LegacyPath, "{not json");

        using (Vault vault = new(folder.LegacyPath))
        {
            Assert.Empty(vault.Data.Entries);
        }

        Assert.True(File.Exists(folder.LegacyPath));
        Assert.False(File.Exists(folder.LegacyPath + ".imported"));
    }

    [Fact]
    public void RapidSavesAllLand()
    {
        using VaultFolder folder = new();

        using (Vault vault = new(folder.DatabasePath))
        {
            Todo todo = new() { Title = "Checkbox" };
            vault.Data.Todos.Add(todo);

            for (int toggle = 0; toggle < 500; toggle++)
            {
                todo.Done = !todo.Done;
                vault.Save();
            }

            vault.Flush();

            using Vault observer = new(folder.DatabasePath);
            Assert.Single(observer.Data.Todos);
            Assert.Equal(todo.Done, observer.Data.Todos[0].Done);
        }

        using Vault reopened = new(folder.DatabasePath);
        Assert.Single(reopened.Data.Todos);
        Assert.False(reopened.Data.Todos[0].Done);
    }

    [Fact]
    public void ConcurrentSavesLandExactlyOnce()
    {
        using VaultFolder folder = new();

        using (Vault vault = new(folder.DatabasePath))
        {
            for (int index = 0; index < 50; index++)
            {
                vault.Data.Cards.Add(new BoardCard { Title = $"Card {index}", Order = index });
            }

            Parallel.For(0, 32, _ => vault.Save());
            vault.Flush();
        }

        using Vault reloaded = new(folder.DatabasePath);

        Assert.Equal(50, reloaded.Data.Cards.Count);
        Assert.Equal("Card 0", reloaded.Data.Cards[0].Title);
        Assert.Equal("Card 49", reloaded.Data.Cards[49].Title);
    }

    [Fact]
    public void PendingWritesFlushOnDispose()
    {
        using VaultFolder folder = new();

        using (Vault vault = new(folder.DatabasePath))
        {
            vault.Data.Reminders.Add(new Reminder { Title = "Ship it", Due = new DateOnly(2026, 8, 13) });
            vault.Save();
        }

        using Vault reloaded = new(folder.DatabasePath);

        Assert.Equal("Ship it", reloaded.Data.Reminders[0].Title);
    }

    [Fact]
    public void GranularWritesPersistWithoutSave()
    {
        using VaultFolder folder = new();
        Meal lunch = new() { Title = "Soup", Day = new DateOnly(2026, 8, 13), Slot = MealSlot.Lunch };

        using (Vault vault = new(folder.DatabasePath))
        {
            Meal dinner = new() { Title = "Stew", Day = new DateOnly(2026, 8, 13), Slot = MealSlot.Dinner };
            vault.Upsert(lunch);
            vault.Upsert(dinner);
            vault.Delete(dinner);
        }

        using Vault reloaded = new(folder.DatabasePath);

        Assert.Single(reloaded.Data.Meals);
        Assert.Equal("Soup", reloaded.Data.Meals[0].Title);
        Assert.Equal(lunch.Id, reloaded.Data.Meals[0].Id);
    }

    [Fact]
    public void SaveRaisesChanged()
    {
        using VaultFolder folder = new();
        using Vault vault = new(folder.DatabasePath);

        int raised = 0;
        vault.Changed += (_, _) => raised++;

        vault.Data.Wishes.Add(new WishItem { Title = "Lamp" });
        vault.Save();
        vault.Save();

        Assert.Equal(2, raised);
    }

    [Fact]
    public void SaveAfterDisposeThrows()
    {
        using VaultFolder folder = new();
        Vault vault = new(folder.DatabasePath);
        vault.Dispose();
        vault.Dispose();

        _ = Assert.Throws<ObjectDisposedException>(vault.Save);
    }

    private static void WriteLegacyVault(string path)
    {
        VaultData legacy = new();
        legacy.Entries.Add(new LedgerEntry
        {
            Title = "Rent",
            Money = Money.Lira(15000),
            Kind = EntryKind.Payment,
            Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 11)),
            CategoryId = "housing",
            Tags = ["fixed"],
            Settled = [new DateOnly(2026, 8, 11)],
        });
        legacy.Todos.Add(new Todo { Title = "Call dentist", Due = new DateOnly(2026, 8, 20) });
        legacy.Plans.Add(new Plan { Title = "Trip", Steps = [new PlanStep { Title = "Book" }] });

        File.WriteAllText(path, JsonSerializer.Serialize(legacy));
    }

    private sealed class VaultFolder : IDisposable
    {
        private readonly string _root;

        internal VaultFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), $"alfred-vault-{Guid.NewGuid():N}");
            _ = Directory.CreateDirectory(_root);
        }

        internal string DatabasePath => Path.Combine(_root, "alfred.db");

        internal string LegacyPath => Path.Combine(_root, "alfred.json");

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
