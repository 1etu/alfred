using System.Windows;
using System.Windows.Controls;
using Alfred.App.ViewModels;

namespace Alfred.App.Controls;

public partial class ViewToolbar : UserControl
{
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions), typeof(IReadOnlyList<ToolbarAction>), typeof(ViewToolbar));

    public ViewToolbar()
    {
        InitializeComponent();
    }

    public IReadOnlyList<ToolbarAction>? Actions
    {
        get => (IReadOnlyList<ToolbarAction>?)GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    private void OnActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ToolbarAction action })
        {
            action.Invoke();
        }
    }
}
