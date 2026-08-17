using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.App.Preferences;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public enum PaymentsFilter
{
    All,
    Out,
    Recurring,
    In,
}

public sealed class PaymentsViewModel : PageViewModel
{
    private readonly Vault _vault;
    private readonly UserPreferences _preferences;

    public PaymentsViewModel(Vault vault, UserPreferences preferences)
        : base(LocalizationService.Text(LocalizationKeys.NavPayments), "PaymentsIcon")
    {
        _vault = vault;
        _preferences = preferences;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionCopy), "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

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

    public void Add(string title, decimal amount, string? currency, EntryKind kind, Cadence cadence, DateOnly anchor, string? brandSlug)
    {
        _vault.Data.Entries.Add(new LedgerEntry
        {
            Title = title,
            Money = new Money(amount, currency ?? _preferences.DefaultCurrency),
            Kind = kind,
            Schedule = cadence == Cadence.None ? Schedule.Once(anchor) : Schedule.Every(cadence, anchor),
            BrandSlug = brandSlug,
        });

        _vault.Save();
    }

    internal void Remove(LedgerEntry entry)
    {
        Recycler.Delete(_vault.Data, entry);
        _vault.Save();
    }

    private void CopyToClipboard() =>
        Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  {row.Amount}  ({row.Meta})")));

    private void Refresh()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly monthEnd = new(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        string home = _preferences.DefaultCurrency;

        decimal expectedIn = 0;
        decimal expectedOut = 0;
        decimal spent = 0;

        foreach (LedgerEntry entry in _vault.Data.Entries.Where(entry => entry.Money.Currency == home))
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

        Summary = today.ToString("MMMM", LocalizationService.Current.Culture)
            + "   ·   "
            + LocalizationService.Text(
                LocalizationKeys.PaymentsSummary,
                MoneyFormat.Compact(new Money(expectedIn, home)),
                MoneyFormat.Compact(new Money(expectedOut, home)),
                MoneyFormat.Compact(new Money(spent, home)));

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
            string cadence = LocalizationService.Text(entry.Schedule.Cadence switch
            {
                Cadence.Weekly => LocalizationKeys.CadenceWeekly,
                Cadence.Monthly => LocalizationKeys.CadenceMonthly,
                Cadence.Yearly => LocalizationKeys.CadenceYearly,
                _ => LocalizationKeys.CadenceOnce,
            });

            if (next is not { } date)
            {
                return cadence;
            }

            string when = date == today
                ? LocalizationService.Text(LocalizationKeys.PaymentsNextToday)
                : date == today.AddDays(1)
                    ? LocalizationService.Text(LocalizationKeys.PaymentsNextTomorrow)
                    : LocalizationService.Text(
                        LocalizationKeys.PaymentsNext,
                        date.ToString("d MMM", LocalizationService.Current.Culture));

            return cadence + " · " + when;
        }
    }
}
