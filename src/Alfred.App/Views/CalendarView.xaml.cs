using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class CalendarView : UserControl
{
    public CalendarView()
    {
        InitializeComponent();
    }

    private void OnPrevious(object sender, RoutedEventArgs e)
    {
        if (DataContext is CalendarViewModel calendar)
        {
            calendar.PreviousMonth();
        }
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (DataContext is CalendarViewModel calendar)
        {
            calendar.NextMonth();
        }
    }
}
