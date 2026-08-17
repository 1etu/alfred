using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class AgendaView : UserControl
{
    public AgendaView()
    {
        InitializeComponent();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is AgendaViewModel model && Bar.Title.Length > 0)
        {
            model.QuickAdd(Bar.Title, Bar.PickedDate ?? DateOnly.FromDateTime(DateTime.Now));
            Bar.Reset();
            Bar.FocusTitle();
        }
    }
}
