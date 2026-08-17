using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class Sidebar : UserControl
{
    public static readonly DependencyProperty ShowsCountsProperty = DependencyProperty.Register(
        nameof(ShowsCounts), typeof(bool), typeof(Sidebar), new PropertyMetadata(true));

    public Sidebar()
    {
        InitializeComponent();
    }

    public bool ShowsCounts
    {
        get => (bool)GetValue(ShowsCountsProperty);
        set => SetValue(ShowsCountsProperty, value);
    }
}
