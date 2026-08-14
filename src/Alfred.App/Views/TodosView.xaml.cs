using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class TodosView : UserControl
{
    private TodosViewModel? _bound;

    public TodosView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            _bound?.PrimaryRequested -= OnPrimaryRequested;
            _bound = DataContext as TodosViewModel;
            _bound?.PrimaryRequested += OnPrimaryRequested;
        };
    }

    private void OnPrimaryRequested(object? sender, EventArgs e) => Bar.FocusTitle();

    private void OnQuickAdd(object sender, EventArgs e)
    {
        if (DataContext is not TodosViewModel todos || Bar.Title.Length == 0)
        {
            return;
        }

        todos.Add(Bar.Title, Bar.PickedDate);
        Bar.Reset();
        Bar.FocusTitle();
    }

    private void OnRemoveRow(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodosViewModel.TodoRow row })
        {
            row.Remove();
        }
    }
}
