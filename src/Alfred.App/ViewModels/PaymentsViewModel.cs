using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Agenda;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;

namespace Alfred.App.ViewModels;

public enum PaymentsFilter
{
    All,
    Out,
    Recurring,
    In,
}

public sealed class PaymentsViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public PaymentsViewModel(Vault vault)
    {
        _vault = vault;
        Actions =
        [
            new ToolbarAction("Copy list", "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => "New entry";

    public event EventHandler? PrimaryRequested;

    public void InvokePrimary() => PrimaryRequested?.Invoke(this, EventArgs.Empty);

    private void CopyToClipboard()
    {
        IEnumerable<string> lines = Rows.Select(row => $"- {row.Title}  {row.Amount}  ({row.Meta})");
        Interop.Clipboards.Set(string.Join(Environment.NewLine, lines));
    }

    public ObservableCollection<EntryRow> Rows { get; } = [];

    public string Summary { get; private set; } = string.Empty;

    public bool IsEmpty => Rows.Count == 0;

    public PaymentsFilter Filter
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                Refresh();
                Raise(nameof(IsAll));
                Raise(nameof(IsOut));
                Raise(nameof(IsRecurring));
                Raise(nameof(IsIn));
            }
        }
    }

    public bool IsAll { get => Filter == PaymentsFilter.All; set { if (value) { Filter = PaymentsFilter.All; } } }

    public bool IsOut { get => Filter == PaymentsFilter.Out; set { if (value) { Filter = PaymentsFilter.Out; } } }

    public bool IsRecurring { get => Filter == PaymentsFilter.Recurring; set { if (value) { Filter = PaymentsFilter.Recurring; } } }

    public bool IsIn { get => Filter == PaymentsFilter.In; set { if (value) { Filter = PaymentsFilter.In; } } }

    public void Add(string title, decimal amount, EntryKind kind, Cadence cadence, DateOnly anchor, string? brandSlug, string? categoryId)
    {
        _vault.Data.Entries.Add(new LedgerEntry
        {
            Title = title,
            Money = Money.Lira(amount),
            Kind = kind,
            Schedule = cadence == Cadence.None ? Schedule.Once(anchor) : Schedule.Every(cadence, anchor),
            BrandSlug = brandSlug,
            CategoryId = categoryId,
        });

        _vault.Save();
    }

    internal void Remove(LedgerEntry entry)
    {
        Recycler.Delete(_vault.Data, entry);
        _vault.Save();
    }

    private void Refresh()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly monthEnd = new(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        decimal expectedIn = 0;
        decimal expectedOut = 0;
        decimal spent = 0;

        foreach (LedgerEntry entry in _vault.Data.Entries)
        {
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(monthStart, monthEnd))
            {
                if (entry.Flow == CashFlow.In)
                {
                    expectedIn += entry.Money.Amount;
                }
                else
                {
                    expectedOut += entry.Money.Amount;
                    if (occurrence <= today)
                    {
                        spent += entry.Money.Amount;
                    }
                }
            }
        }

        Summary = $"{today:MMMM}   ·   expecting {MoneyFormat.Compact(Money.Lira(expectedIn))} in · {MoneyFormat.Compact(Money.Lira(expectedOut))} out · {MoneyFormat.Compact(Money.Lira(spent))} gone so far";

        Rows.Clear();

        IEnumerable<LedgerEntry> entries = _vault.Data.Entries.Where(entry => Filter switch
        {
            PaymentsFilter.Out => entry.Flow == CashFlow.Out,
            PaymentsFilter.In => entry.Flow == CashFlow.In,
            PaymentsFilter.Recurring => entry.Schedule.IsRecurring,
            _ => true,
        });

        foreach (LedgerEntry entry in entries.OrderBy(NextDate).ThenBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase))
        {
            Rows.Add(new EntryRow(entry, this, today));
        }

        Raise(nameof(Summary));
        Raise(nameof(IsEmpty));
    }

    private static DateOnly NextDate(LedgerEntry entry)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        foreach (DateOnly occurrence in entry.Schedule.Occurrences(today, today.AddDays(400)))
        {
            return occurrence;
        }

        return DateOnly.MaxValue;
    }

    public sealed class EntryRow
    {
        private readonly PaymentsViewModel _owner;

        public EntryRow(LedgerEntry entry, PaymentsViewModel owner, DateOnly today)
        {
            Entry = entry;
            _owner = owner;

            DateOnly? next = null;
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(today, today.AddDays(400)))
            {
                next = occurrence;
                break;
            }

            Meta = BuildMeta(entry, next, today);
        }

        public LedgerEntry Entry { get; }

        public string Title => Entry.Title;

        public string Meta { get; }

        public string Amount => MoneyFormat.WithSign(Entry.Money, Entry.Flow);

        public bool IsIncome => Entry.Flow == CashFlow.In;

        public string? BrandSlug => Entry.BrandSlug;

        public void Remove() => _owner.Remove(Entry);

        private static string BuildMeta(LedgerEntry entry, DateOnly? next, DateOnly today)
        {
            string cadence = entry.Schedule.Cadence switch
            {
                Cadence.Weekly => "Weekly",
                Cadence.Monthly => "Monthly",
                Cadence.Yearly => "Yearly",
                _ => "Once",
            };

            if (next is not { } date)
            {
                return cadence;
            }

            string when = date == today
                ? "today"
                : date == today.AddDays(1) ? "tomorrow" : date.ToString("d MMM", CultureInfo.InvariantCulture);

            return $"{cadence} · next {when}";
        }
    }
}
