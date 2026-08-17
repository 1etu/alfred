using System.Globalization;
using Alfred.Localization;

namespace Alfred.UIKit.Controls;

public sealed class DayStripDay : Observable
{
    private readonly DayStrip _owner;

    internal DayStripDay(DateOnly date, DayStrip owner)
    {
        Date = date;
        _owner = owner;
    }

    public DateOnly Date { get; }

    public string Name => Date.ToString("ddd", LocalizationService.Current.Culture);

    public string Number => Date.Day.ToString(CultureInfo.InvariantCulture);

    public bool IsSelected
    {
        get => _owner.SelectedDay == Date;
        set
        {
            if (value)
            {
                _owner.SelectedDay = Date;
            }
        }
    }

    internal void RefreshSelected() => Raise(nameof(IsSelected));
}
