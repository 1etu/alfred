using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Agenda;
using Alfred.Core.Items;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public enum AgendaMode
{
    Today,
    Upcoming,
}

public sealed class AgendaViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public AgendaViewModel(Vault vault, AgendaMode mode)
    {
        _vault = vault;
        Mode = mode;
        Actions =
        [
            new ToolbarAction("Copy list", "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => Mode == AgendaMode.Today ? "Add to today" : null;

    public event EventHandler? PrimaryRequested;

    public void InvokePrimary() => PrimaryRequested?.Invoke(this, EventArgs.Empty);

    private void CopyToClipboard()
    {
        IEnumerable<string> lines = Rows
            .OfType<AgendaRow>()
            .Select(row => row.Amount is { } amount ? $"- {row.Title}  {amount}" : "- " + row.Title);

        Interop.Clipboards.Set(string.Join(Environment.NewLine, lines));
    }

    public AgendaMode Mode { get; }

    public string Title => Mode == AgendaMode.Today ? "Today" : "Upcoming";

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

    private void Refresh()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        Rows.Clear();

        if (Mode == AgendaMode.Today)
        {
            DayMoney money = AgendaService.MoneyOn(_vault.Data, today);
            Subtitle = today.ToString("dddd, d MMMM", CultureInfo.InvariantCulture);
            Greeting = BuildGreeting();

            IReadOnlyList<AgendaItem> items = AgendaService.Today(_vault.Data, today);

            StatMoney = MoneyFormat.Compact(Money.Lira(money.Out)) + " out";
            StatMoneyDetail = MoneyFormat.Compact(Money.Lira(money.In)) + " in today";

            int due = items.Count(item => !item.IsDone && item.Kind != AgendaKind.Know);
            StatDue = due == 0 ? "All clear" : due + (due == 1 ? " thing" : " things");
            StatDueDetail = due == 0 ? "nothing needs you" : "waiting on you today";

            AgendaItem? next = AgendaService.Upcoming(_vault.Data, today, 90)
                .FirstOrDefault(item => item.Flow == CashFlow.Out && item.Money is not null);

            HasNext = next is not null;
            if (next is not null)
            {
                int days = next.Date.DayNumber - today.DayNumber;
                StatNext = next.Title;
                StatNextDetail = MoneyFormat.Compact(next.Money!.Value) + " · in " + days + (days == 1 ? " day" : " days");
            }

            bool anyOverdue = items.Any(item => item.IsOverdue);
            if (anyOverdue)
            {
                Rows.Add(new DayHeaderRow("Overdue"));
            }

            foreach (AgendaItem item in items.Where(item => item.IsOverdue))
            {
                Rows.Add(new AgendaRow(item, _vault));
            }

            if (items.Any(item => !item.IsOverdue))
            {
                Rows.Add(new DayHeaderRow("Today"));
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
        else
        {
            Subtitle = "The next 90 days";
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

    private static string BuildGreeting()
    {
        int hour = DateTime.Now.Hour;

        string opener = hour switch
        {
            < 6 => "Still up, Ege?",
            < 12 => "Good morning, Ege",
            < 18 => "Good afternoon, Ege",
            _ => "Good evening, Ege",
        };

        return opener + " 👋";
    }

    private static string HeaderFor(DateOnly day, DateOnly today)
    {
        int distance = day.DayNumber - today.DayNumber;

        return distance switch
        {
            1 => "Tomorrow · " + day.ToString("dddd d MMMM", CultureInfo.InvariantCulture),
            < 7 => day.ToString("dddd d MMMM", CultureInfo.InvariantCulture),
            _ => day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
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
        public DayHeaderRow(string header)
        {
            Header = header;
        }

        public string Header { get; }
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
            ? _item.Source + " · " + _item.Date.ToString("d MMM", CultureInfo.InvariantCulture)
            : _item.Source;

        public string OverdueLabel => _item.Date.ToString("d MMM", CultureInfo.InvariantCulture);

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
