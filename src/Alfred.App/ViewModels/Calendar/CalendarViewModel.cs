using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Agenda;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;

namespace Alfred.App.ViewModels;

public sealed class CalendarViewModel : PageViewModel
{
    private readonly Vault _vault;
    private DateOnly _month;
    private DateOnly _selected;

    public CalendarViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavCalendar), "CalendarIcon")
    {
        _vault = vault;
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        _month = new DateOnly(today.Year, today.Month, 1);
        _selected = today;
        WeekdayNames = BuildWeekdayNames();

        _vault.Changed += (_, _) => Build();
        Build();
    }

    public ObservableCollection<DayCell> Cells { get; } = [];

    public ObservableCollection<AgendaViewModel.AgendaRow> DayItems { get; } = [];

    public string MonthTitle => _month.ToString("MMMM yyyy", LocalizationService.Current.Culture);

    public string SelectedTitle => _selected.ToString("dddd, d MMMM", LocalizationService.Current.Culture);

    public bool DayIsEmpty => DayItems.Count == 0;

    public IReadOnlyList<string> WeekdayNames { get; }

    internal DateOnly Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                foreach (DayCell cell in Cells)
                {
                    cell.RefreshSelected();
                }

                BuildDay();
                Raise(nameof(SelectedTitle));
            }
        }
    }

    public void PreviousMonth() => ShiftMonth(-1);

    public void NextMonth() => ShiftMonth(1);

    public void GoToToday()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        _month = new DateOnly(today.Year, today.Month, 1);
        Build();
        Raise(nameof(MonthTitle));
        Selected = today;
    }

    private void ShiftMonth(int months)
    {
        _month = _month.AddMonths(months);
        Build();
        Raise(nameof(MonthTitle));
    }

    private static List<string> BuildWeekdayNames()
    {
        DateTimeFormatInfo format = LocalizationService.Current.Culture.DateTimeFormat;
        List<string> names = [];

        for (int offset = 0; offset < 7; offset++)
        {
            names.Add(format.AbbreviatedDayNames[((int)DayOfWeek.Monday + offset) % 7]);
        }

        return names;
    }

    private void Build()
    {
        Cells.Clear();
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        int lead = ((int)_month.DayOfWeek + 6) % 7;
        DateOnly cursor = _month.AddDays(-lead);

        for (int index = 0; index < 42; index++)
        {
            bool inMonth = cursor.Month == _month.Month;
            bool busy = AgendaService.On(_vault.Data, cursor).Count > 0;
            Cells.Add(new DayCell(cursor, inMonth, cursor == today, busy, this));
            cursor = cursor.AddDays(1);
        }

        BuildDay();
    }

    private void BuildDay()
    {
        DayItems.Clear();

        foreach (AgendaItem item in AgendaService.On(_vault.Data, _selected))
        {
            DayItems.Add(new AgendaViewModel.AgendaRow(item, _vault));
        }

        Raise(nameof(DayIsEmpty));
    }

    public sealed class DayCell : Observable
    {
        private readonly CalendarViewModel _owner;

        public DayCell(DateOnly day, bool isInMonth, bool isToday, bool isBusy, CalendarViewModel owner)
        {
            Day = day;
            IsInMonth = isInMonth;
            IsToday = isToday;
            IsBusy = isBusy;
            _owner = owner;
        }

        public DateOnly Day { get; }

        public string Number => Day.Day.ToString(CultureInfo.InvariantCulture);

        public bool IsInMonth { get; }

        public bool IsToday { get; }

        public bool IsBusy { get; }

        public bool IsSelected
        {
            get => _owner.Selected == Day;
            set
            {
                if (value)
                {
                    _owner.Selected = Day;
                }
            }
        }

        internal void RefreshSelected() => Raise(nameof(IsSelected));
    }
}
