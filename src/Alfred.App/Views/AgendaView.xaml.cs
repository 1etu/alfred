using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class AgendaView : UserControl
{
    public static readonly DependencyProperty IsTodayProperty = DependencyProperty.Register(
        nameof(IsToday), typeof(bool), typeof(AgendaView));

    public static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(
        nameof(HeaderIcon), typeof(System.Windows.Media.ImageSource), typeof(AgendaView));

    private AgendaViewModel? _bound;

    public AgendaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public bool IsToday
    {
        get => (bool)GetValue(IsTodayProperty);
        set => SetValue(IsTodayProperty, value);
    }

    public System.Windows.Media.ImageSource? HeaderIcon
    {
        get => (System.Windows.Media.ImageSource?)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _bound?.PrimaryRequested -= OnPrimaryRequested;

        _bound = DataContext as AgendaViewModel;
        IsToday = _bound is { Mode: AgendaMode.Today };
        HeaderIcon = Application.Current.Resources[IsToday ? "TodayIcon" : "UpcomingIcon"] as System.Windows.Media.ImageSource;

        _bound?.PrimaryRequested += OnPrimaryRequested;
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => Bar.FocusTitle();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not AgendaViewModel agenda || Bar.Title.Length == 0)
        {
            return;
        }

        agenda.QuickAdd(Bar.Title, Bar.PickedDate ?? DateOnly.FromDateTime(DateTime.Now));
        Bar.Reset();
        Bar.FocusTitle();
    }
}
