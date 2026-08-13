using Alfred.Core.Agenda;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Xunit;

namespace Alfred.Core.Tests.Agenda;

public sealed class AgendaServiceTests
{
    private static readonly DateOnly Today = new(2026, 8, 13);

    private static VaultData Sample()
    {
        VaultData data = new();
        data.Todos.Add(new Todo { Title = "Overdue thing", Due = Today.AddDays(-2) });
        data.Todos.Add(new Todo { Title = "For today", Due = Today });
        data.Entries.Add(new LedgerEntry
        {
            Title = "Netflix",
            Money = Money.Lira(200),
            Kind = EntryKind.Expense,
            Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 1, 13)),
        });
        data.Entries.Add(new LedgerEntry
        {
            Title = "Freelance",
            Money = Money.Lira(32000),
            Kind = EntryKind.Income,
            Schedule = Schedule.Every(Cadence.Weekly, new DateOnly(2026, 8, 7)),
        });
        data.Entries.Add(new LedgerEntry
        {
            Title = "Mom",
            Money = Money.Lira(15000),
            Kind = EntryKind.Payment,
            Schedule = Schedule.Every(Cadence.Monthly, new DateOnly(2026, 8, 11)),
        });
        return data;
    }

    [Fact]
    public void TodayPinsOverdueFirstAndIncludesUnsettledPayment()
    {
        IReadOnlyList<AgendaItem> items = AgendaService.Today(Sample(), Today);

        Assert.True(items[0].IsOverdue);
        Assert.Contains(items, item => item is { Title: "Mom", Kind: AgendaKind.Settle, IsOverdue: true });
        Assert.Contains(items, item => item is { Title: "Netflix", Kind: AgendaKind.Know });
        Assert.Contains(items, item => item is { Title: "For today", Kind: AgendaKind.Do });
    }

    [Fact]
    public void SettledPaymentIsNotOverdue()
    {
        VaultData data = Sample();
        data.Entries.First(entry => entry.Title == "Mom").Settle(new DateOnly(2026, 8, 11), true);

        IReadOnlyList<AgendaItem> items = AgendaService.Today(data, Today);

        Assert.DoesNotContain(items, item => item.Title == "Mom" && item.IsOverdue);
    }

    [Fact]
    public void UpcomingSkipsEmptyDaysAndStaysInsideHorizon()
    {
        IReadOnlyList<AgendaItem> items = AgendaService.Upcoming(Sample(), Today, 31);

        Assert.All(items, item => Assert.True(item.Date > Today && item.Date <= Today.AddDays(31)));
        Assert.Contains(items, item => item.Title == "Freelance" && item.Date == new DateOnly(2026, 8, 14));
        Assert.Contains(items, item => item.Title == "Netflix" && item.Date == new DateOnly(2026, 9, 13));
    }

    [Fact]
    public void MoneyOnSumsBothDirections()
    {
        DayMoney money = AgendaService.MoneyOn(Sample(), new DateOnly(2026, 8, 14));

        Assert.Equal(0, money.Out);
        Assert.Equal(32000, money.In);
    }
}
