using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class DayStrip : UserControl
{
    public static readonly DependencyProperty FirstDayProperty = DependencyProperty.Register(
        nameof(FirstDay), typeof(DateOnly), typeof(DayStrip),
        new PropertyMetadata(default(DateOnly), OnShapeChanged));

    public static readonly DependencyProperty DayCountProperty = DependencyProperty.Register(
        nameof(DayCount), typeof(int), typeof(DayStrip),
        new PropertyMetadata(7, OnShapeChanged));

    public static readonly DependencyProperty SelectedDayProperty = DependencyProperty.Register(
        nameof(SelectedDay), typeof(DateOnly), typeof(DayStrip),
        new FrameworkPropertyMetadata(default(DateOnly), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDayChanged));

    private readonly ObservableCollection<DayStripDay> _days = [];

    public DayStrip()
    {
        InitializeComponent();
        Days.ItemsSource = _days;
    }

    public DateOnly FirstDay
    {
        get => (DateOnly)GetValue(FirstDayProperty);
        set => SetValue(FirstDayProperty, value);
    }

    public int DayCount
    {
        get => (int)GetValue(DayCountProperty);
        set => SetValue(DayCountProperty, value);
    }

    public DateOnly SelectedDay
    {
        get => (DateOnly)GetValue(SelectedDayProperty);
        set => SetValue(SelectedDayProperty, value);
    }

    private void Rebuild()
    {
        _days.Clear();

        DateOnly first = FirstDay == default ? DateOnly.FromDateTime(DateTime.Now) : FirstDay;

        for (int offset = 0; offset < Math.Max(DayCount, 1); offset++)
        {
            _days.Add(new DayStripDay(first.AddDays(offset), this));
        }
    }

    private static void OnShapeChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((DayStrip)target).Rebuild();

    private static void OnSelectedDayChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        foreach (DayStripDay day in ((DayStrip)target)._days)
        {
            day.RefreshSelected();
        }
    }
}
