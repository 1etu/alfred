using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class ParseChip : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(ParseChip),
        new PropertyMetadata(null, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(ParseChip));

    public ParseChip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public event EventHandler? Dismissed;

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Motion.Rise(this, 4, 140);
        Motion.Shimmer(this);
    }

    private void OnDismiss(object sender, RoutedEventArgs e) => Dismissed?.Invoke(this, EventArgs.Empty);

    private static void OnLabelChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((ParseChip)target).LabelText.Visibility = e.NewValue is string { Length: > 0 }
            ? Visibility.Visible
            : Visibility.Collapsed;
}
