using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class AgendaView : UserControl
{
    public static readonly DependencyProperty IsTodayProperty = DependencyProperty.Register(
        nameof(IsToday), typeof(bool), typeof(AgendaView));

    private AgendaViewModel? _bound;

    public AgendaView()
    {
        InitializeComponent();
        QuickTitle.Source = new DateSource();
        DataContextChanged += OnDataContextChanged;
    }

    public bool IsToday
    {
        get => (bool)GetValue(IsTodayProperty);
        set => SetValue(IsTodayProperty, value);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_bound is not null)
        {
            _bound.PrimaryRequested -= OnPrimaryRequested;
        }

        _bound = DataContext as AgendaViewModel;
        IsToday = _bound is { Mode: AgendaMode.Today };

        if (_bound is not null)
        {
            _bound.PrimaryRequested += OnPrimaryRequested;
        }
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => QuickTitle.FocusInput();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not AgendaViewModel agenda || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        agenda.QuickAdd(QuickTitle.Text.Trim(), DateOnly.FromDateTime(DateTime.Now));
        QuickTitle.Text = string.Empty;
        QuickTitle.FocusInput();
    }
}
