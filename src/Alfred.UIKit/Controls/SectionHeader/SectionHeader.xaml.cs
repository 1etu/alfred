using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(SectionHeader));

    public static readonly DependencyProperty TintKeyProperty = DependencyProperty.Register(
        nameof(TintKey), typeof(string), typeof(SectionHeader),
        new PropertyMetadata(null, OnTintKeyChanged));

    public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
        nameof(Trailing), typeof(object), typeof(SectionHeader));

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? TintKey
    {
        get => (string?)GetValue(TintKeyProperty);
        set => SetValue(TintKeyProperty, value);
    }

    public object? Trailing
    {
        get => GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    private static void OnTintKeyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((SectionHeader)target).TitleText.SetResourceReference(
            TextBlock.ForegroundProperty,
            e.NewValue is string key ? key : "TextSecondary");
}
