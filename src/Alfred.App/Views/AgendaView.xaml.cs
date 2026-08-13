using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class AgendaView : UserControl
{
    public static readonly DependencyProperty IsTodayProperty = DependencyProperty.Register(
        nameof(IsToday), typeof(bool), typeof(AgendaView));

    private DateOnly? _pickedDate;

    public AgendaView()
    {
        InitializeComponent();
        QuickDate.Source = new DateSource();
        QuickDate.Committed += (_, suggestion) => _pickedDate = suggestion.Value as DateOnly?;
        DataContextChanged += (_, _) =>
            IsToday = DataContext is AgendaViewModel { Mode: AgendaMode.Today };
    }

    public bool IsToday
    {
        get => (bool)GetValue(IsTodayProperty);
        set => SetValue(IsTodayProperty, value);
    }

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not AgendaViewModel agenda || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        agenda.QuickAdd(QuickTitle.Text.Trim(), _pickedDate ?? DateOnly.FromDateTime(DateTime.Now));
        QuickTitle.Text = string.Empty;
        QuickDate.Text = string.Empty;
        _pickedDate = null;
        QuickTitle.FocusInput();
    }
}
