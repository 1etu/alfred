using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.App.Preferences;
using Alfred.Core.Agenda;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public enum AgendaMode
{
    Today,
    Upcoming,
}

public sealed class AgendaViewModel : PageViewModel
{
    private readonly Vault _vault;
    private readonly UserPreferences _preferences;

    public AgendaViewModel(Vault vault, AgendaMode mode, UserPreferences preferences)
        : base(
            LocalizationService.Text(mode == AgendaMode.Today ? LocalizationKeys.NavToday : LocalizationKeys.NavUpcoming),
            mode == AgendaMode.Today ? "TodayIcon" : "UpcomingIcon")
    {
        _vault = vault;
        _preferences = preferences;
        Mode = mode;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionCopy), "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

    public AgendaMode Mode { get; }

    public bool IsToday => Mode == AgendaMode.Today;

    public string Subtitle { get; private set; } = string.Empty;

    public string Greeting { get; private set; } = string.Empty;

    public string StatMoney { get; private set; } = string.Empty;

    public string StatMoneyDetail { get; private set; } = string.Empty;

    public string StatDue { get; private set; } = string.Empty;

    public string StatDueDetail { get; private set; } = string.Empty;

    public string StatNext { get; private set; } = string.Empty;

    public string StatNextDetail { get; private set; } = string.Empty;

    public bool HasNext { get; private set; }

    public ObservableCollection<object> Rows { get; } = [];

    public bool IsEmpty => Rows.Count == 0;

    public void QuickAdd(string title, DateOnly due)
    {
        _vault.Data.Todos.Add(new Todo { Title = title, Due = due });
        _vault.Save();
    }

    private void CopyToClipboard()
    {
        IEnumerable<string> lines = Rows
            .OfType<AgendaRow>()
            .Select(row => row.Amount is { } amount ? $"- {row.Title}  {amount}" : "- " + row.Title);

        Clipboards.Set(string.Join(Environment.NewLine, lines));
    }

    private void Refresh()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        Rows.Clear();

        if (Mode == AgendaMode.Today)
        {
            RefreshToday(today);
        }
        else
        {
            Subtitle = LocalizationService.Text(LocalizationKeys.AgendaNext90);
            DateOnly? currentDay = null;

            foreach (AgendaItem item in AgendaService.Upcoming(_vault.Data, today, 90))
            {
                if (item.Date != currentDay)
                {
                    currentDay = item.Date;
                    Rows.Add(new DayHeaderRow(HeaderFor(item.Date, today)));
                }

                Rows.Add(new AgendaRow(item, _vault));
            }
        }

        Raise(nameof(Subtitle));
        Raise(nameof(IsEmpty));
    }

    private void RefreshToday(DateOnly today)
    {
        string home = _preferences.DefaultCurrency;
        DayMoney money = AgendaService.MoneyOn(_vault.Data, today);
        Subtitle = today.ToString("dddd, d MMMM", LocalizationService.Current.Culture);
        Greeting = BuildGreeting();

        IReadOnlyList<AgendaItem> items = AgendaService.Today(_vault.Data, today);

        StatMoney = LocalizationService.Text(LocalizationKeys.StatOut, MoneyFormat.Compact(new Money(money.Out, home)));
        StatMoneyDetail = LocalizationService.Text(LocalizationKeys.StatInToday, MoneyFormat.Compact(new Money(money.In, home)));

        int due = items.Count(item => !item.IsDone && item.Kind != AgendaKind.Know);
        StatDue = due == 0 ? LocalizationService.Text(LocalizationKeys.StatAllClear) : due.ToString(LocalizationService.Current.Culture);
        StatDueDetail = LocalizationService.Text(due == 0 ? LocalizationKeys.StatNothingNeedsYou : LocalizationKeys.StatWaitingToday);

        AgendaItem? next = AgendaService.Upcoming(_vault.Data, today, 90)
            .FirstOrDefault(item => item.Flow == CashFlow.Out && item.Money is not null);

        HasNext = next is not null;
        if (next is not null)
        {
            int days = next.Date.DayNumber - today.DayNumber;
            StatNext = next.Title;
            StatNextDetail = MoneyFormat.Compact(next.Money!.Value) + " · " + (days == 1
                ? LocalizationService.Text(LocalizationKeys.StatInOneDay)
                : LocalizationService.Text(LocalizationKeys.StatInDays, days));
        }

        if (items.Any(item => item.IsOverdue))
        {
            Rows.Add(new DayHeaderRow(LocalizationService.Text(LocalizationKeys.SectionOverdue), isOverdue: true));
        }

        foreach (AgendaItem item in items.Where(item => item.IsOverdue))
        {
            Rows.Add(new AgendaRow(item, _vault));
        }

        if (items.Any(item => !item.IsOverdue))
        {
            Rows.Add(new DayHeaderRow(LocalizationService.Text(LocalizationKeys.NavToday)));
        }

        foreach (AgendaItem item in items.Where(item => !item.IsOverdue))
        {
            Rows.Add(new AgendaRow(item, _vault));
        }

        Raise(nameof(Greeting));
        Raise(nameof(StatMoney));
        Raise(nameof(StatMoneyDetail));
        Raise(nameof(StatDue));
        Raise(nameof(StatDueDetail));
        Raise(nameof(StatNext));
        Raise(nameof(StatNextDetail));
        Raise(nameof(HasNext));
    }

    private static string BuildGreeting()
    {
        int hour = DateTime.Now.Hour;

        return LocalizationService.Text(hour switch
        {
            < 6 => LocalizationKeys.GreetingNight,
            < 12 => LocalizationKeys.GreetingMorning,
            < 18 => LocalizationKeys.GreetingAfternoon,
            _ => LocalizationKeys.GreetingEvening,
        }) + " 👋";
    }

    private static string HeaderFor(DateOnly day, DateOnly today)
    {
        int distance = day.DayNumber - today.DayNumber;

        return distance switch
        {
            1 => LocalizationService.Text(LocalizationKeys.SectionTomorrow) + " · " + day.ToString("dddd d MMMM", LocalizationService.Current.Culture),
            < 7 => day.ToString("dddd d MMMM", LocalizationService.Current.Culture),
            _ => day.ToString("d MMMM yyyy", LocalizationService.Current.Culture),
        };
    }

    public static void Toggle(Vault vault, AgendaItem item, bool done)
    {
        VaultData data = vault.Data;

        if (data.Todos.FirstOrDefault(todo => todo.Id == item.SourceId) is { } todo)
        {
            todo.Done = done;
        }
        else if (data.Reminders.FirstOrDefault(reminder => reminder.Id == item.SourceId) is { } reminder)
        {
            reminder.Done = done;
        }
        else if (data.Entries.FirstOrDefault(entry => entry.Id == item.SourceId) is { } entry)
        {
            entry.Settle(item.Date, done);
        }
        else if (data.Meals.FirstOrDefault(meal => meal.Id == item.SourceId) is { } meal)
        {
            meal.Eaten = done;
        }

        vault.Save();
    }

    public sealed class DayHeaderRow
    {
        public DayHeaderRow(string header, bool isOverdue = false)
        {
            Header = header;
            IsOverdue = isOverdue;
        }

        public string Header { get; }

        public bool IsOverdue { get; }

        public string TintKey => IsOverdue ? "Overdue" : "TextSecondary";
    }

    public sealed class AgendaRow : Observable
    {
        private readonly Vault _vault;
        private readonly AgendaItem _item;

        public AgendaRow(AgendaItem item, Vault vault)
        {
            _item = item;
            _vault = vault;
        }

        public string Title => _item.Title;

        public string Source => _item.Source;

        public string Meta => _item.IsOverdue
            ? _item.Source + " · " + _item.Date.ToString("d MMM", LocalizationService.Current.Culture)
            : _item.Source;

        public string? Amount => _item.Money is { } money && _item.Flow is { } flow
            ? MoneyFormat.WithSign(money, flow)
            : null;

        public bool IsIncome => _item.Flow == CashFlow.In;

        public string? BrandSlug => _item.BrandSlug;

        public bool HasCheck => _item.Kind != AgendaKind.Know;

        public bool IsOverdue => _item.IsOverdue;

        public bool IsDone
        {
            get => _item.IsDone;
            set
            {
                Toggle(_vault, _item, value);
                Raise();
            }
        }
    }
}
