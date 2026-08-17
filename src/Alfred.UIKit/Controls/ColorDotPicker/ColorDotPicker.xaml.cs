using System.Windows;
using System.Windows.Controls;

namespace Alfred.UIKit.Controls;

public partial class ColorDotPicker : UserControl
{
    public static readonly DependencyProperty SelectedKeyProperty = DependencyProperty.Register(
        nameof(SelectedKey), typeof(string), typeof(ColorDotPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedKeyChanged));

    private bool _isSyncing;

    public ColorDotPicker()
    {
        InitializeComponent();
        Dots.ItemsSource = FolderColors.All;
    }

    public string? SelectedKey
    {
        get => (string?)GetValue(SelectedKeyProperty);
        set => SetValue(SelectedKeyProperty, value);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || Dots.SelectedItem is not FolderColor chosen)
        {
            return;
        }

        _isSyncing = true;
        SelectedKey = chosen.BrushKey;
        _isSyncing = false;
    }

    private static void OnSelectedKeyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        ColorDotPicker picker = (ColorDotPicker)target;

        if (picker._isSyncing)
        {
            return;
        }

        picker._isSyncing = true;
        picker.Dots.SelectedItem = e.NewValue is string key ? FolderColors.Resolve(key) : null;
        picker._isSyncing = false;
    }
}
