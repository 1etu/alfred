using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Alfred.App.ViewModels;
using Alfred.UIKit.Controls;

namespace Alfred.App.Views;

public partial class BoardView : UserControl
{
    public BoardView()
    {
        InitializeComponent();
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed &&
            sender is FrameworkElement { DataContext: BoardViewModel.CardRow row } element)
        {
            DragDrop.DoDragDrop(element, row, DragDropEffects.Move);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(BoardViewModel.CardRow))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BoardViewModel.ColumnModel column } &&
            e.Data.GetData(typeof(BoardViewModel.CardRow)) is BoardViewModel.CardRow row)
        {
            column.Drop(row);
        }
    }

    private void OnAddCard(object sender, EventArgs e)
    {
        if (sender is SmartInput input &&
            input.DataContext is BoardViewModel.ColumnModel column &&
            !string.IsNullOrWhiteSpace(input.Text))
        {
            column.Add(input.Text.Trim());
            input.Text = string.Empty;
            input.FocusInput();
        }
    }

    private void OnRemoveCard(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BoardViewModel.CardRow row })
        {
            row.Remove();
        }
    }
}
