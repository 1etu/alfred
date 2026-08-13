using System.Windows;
using System.Windows.Controls;
using Alfred.App.Suggest;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class TodosView : UserControl
{
    private DateOnly? _pickedDate;

    public TodosView()
    {
        InitializeComponent();
        QuickDate.Source = new DateSource();
        QuickDate.Committed += (_, suggestion) => _pickedDate = suggestion.Value as DateOnly?;
    }

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not TodosViewModel todos || string.IsNullOrWhiteSpace(QuickTitle.Text))
        {
            return;
        }

        todos.Add(QuickTitle.Text.Trim(), _pickedDate);
        QuickTitle.Text = string.Empty;
        QuickDate.Text = string.Empty;
        _pickedDate = null;
        QuickTitle.FocusInput();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodosViewModel.TodoRow row })
        {
            row.Remove();
        }
    }
}
