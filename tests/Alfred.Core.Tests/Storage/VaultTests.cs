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
        string path = Path.Combine(Path.GetTempPath(), $"alfred-vault-{Guid.NewGuid():N}.json");

        try
        {
            Vault vault = new(path);
            vault.Data.Entries.Add(new LedgerEntry
            {
                Title = "Rent",
                Money = Money.Lira(15000),
                Kind = EntryKind.Payment,
                Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 11)),
                CategoryId = "housing",
                Settled = [new DateOnly(2026, 8, 11)],
            });
            vault.Data.Todos.Add(new Todo { Title = "Call dentist", Due = new DateOnly(2026, 8, 20) });
            vault.Data.Reminders.Add(new Reminder { Title = "Standup", Due = new DateOnly(2026, 8, 14), At = new TimeOnly(10, 0) });
            vault.Data.Plans.Add(new Plan { Title = "Trip", Steps = [new PlanStep { Title = "Book" }] });
            vault.Data.Meals.Add(new Meal { Title = "Pasta", Day = new DateOnly(2026, 8, 13), Slot = MealSlot.Dinner });
            vault.Data.Wishes.Add(new WishItem { Title = "Keyboard", Price = Money.Lira(4500) });
            vault.Data.Cards.Add(new BoardCard { Title = "Idea", Column = BoardColumn.Doing, Tint = 2 });
            vault.Save();

            Vault reloaded = new(path);

            Assert.Equal("Rent", reloaded.Data.Entries[0].Title);
            Assert.Equal(Cadence.Monthly, reloaded.Data.Entries[0].Schedule.Cadence);
            Assert.True(reloaded.Data.Entries[0].IsSettled(new DateOnly(2026, 8, 11)));
            Assert.Equal(new DateOnly(2026, 8, 20), reloaded.Data.Todos[0].Due);
            Assert.Equal(new TimeOnly(10, 0), reloaded.Data.Reminders[0].At);
            Assert.Single(reloaded.Data.Plans[0].Steps);
            Assert.Equal(MealSlot.Dinner, reloaded.Data.Meals[0].Slot);
            Assert.Equal(4500, reloaded.Data.Wishes[0].Price!.Value.Amount);
            Assert.Equal(BoardColumn.Doing, reloaded.Data.Cards[0].Column);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CorruptFileFallsBackToEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"alfred-vault-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{not json");

        try
        {
            Vault vault = new(path);
            Assert.Empty(vault.Data.Entries);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
