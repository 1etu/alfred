using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class EmptyState : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(EmptyState),
        new PropertyMetadata(null, OnMessageChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(EmptyState));

    public EmptyState()
    {
        InitializeComponent();
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private static void OnMessageChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is string message)
        {
            ((EmptyState)target).MessageText.Text = message;
        }
    }
}
