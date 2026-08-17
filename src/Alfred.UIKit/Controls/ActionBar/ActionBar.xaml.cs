using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class ActionBar : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IReadOnlyList<ActionBarItem>), typeof(ActionBar));

    public ActionBar()
    {
        InitializeComponent();
    }

    public IReadOnlyList<ActionBarItem>? Items
    {
        get => (IReadOnlyList<ActionBarItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ActionBarItem item })
        {
            item.Invoke();
        }
    }
}
