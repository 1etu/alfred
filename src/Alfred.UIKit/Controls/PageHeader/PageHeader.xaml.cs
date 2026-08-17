using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alfred.UIKit.Controls;

public partial class PageHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(PageHeader));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(ImageSource), typeof(PageHeader));

    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
        nameof(Count), typeof(string), typeof(PageHeader));

    public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
        nameof(Trailing), typeof(object), typeof(PageHeader));

    public PageHeader()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Count
    {
        get => (string?)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public object? Trailing
    {
        get => GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }
}
