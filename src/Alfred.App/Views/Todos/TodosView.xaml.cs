using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class TodosView : UserControl
{
    public TodosView()
    {
        InitializeComponent();
    }

    private void OnQuickAdd(object? sender, EventArgs e)
    {
        if (DataContext is TodosViewModel model && Bar.Title.Length > 0)
        {
            model.Add(Bar.Title, Bar.PickedDate);
            Bar.Reset();
            Bar.FocusTitle();
        }
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodosViewModel.TodoRow row })
        {
            row.Remove();
        }
    }
}
