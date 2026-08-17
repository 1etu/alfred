using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class KeycapHintBar : UserControl
{
    public static readonly DependencyProperty HintsProperty = DependencyProperty.Register(
        nameof(Hints), typeof(IReadOnlyList<KeyHint>), typeof(KeycapHintBar));

    public KeycapHintBar()
    {
        InitializeComponent();
    }

    public IReadOnlyList<KeyHint>? Hints
    {
        get => (IReadOnlyList<KeyHint>?)GetValue(HintsProperty);
        set => SetValue(HintsProperty, value);
    }
}
