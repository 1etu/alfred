using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Alfred.App.Controls;

public partial class EditableText : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(EditableText),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private bool _isEditing;

    public EditableText()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void OnDisplayClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            return;
        }

        _isEditing = true;
        Editor.Text = Text ?? string.Empty;
        Display.Visibility = Visibility.Collapsed;
        Editor.Visibility = Visibility.Visible;
        Editor.Focus();
        Editor.SelectAll();
        e.Handled = true;
    }

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitEdit();
                e.Handled = true;
                break;

            case Key.Escape:
                CancelEdit();
                e.Handled = true;
                break;
        }
    }

    private void OnEditorLostFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitEdit();

    private void CommitEdit()
    {
        if (!_isEditing)
        {
            return;
        }

        _isEditing = false;
        string edited = Editor.Text.Trim();

        if (edited.Length > 0 && edited != Text)
        {
            Text = edited;
        }

        Editor.Visibility = Visibility.Collapsed;
        Display.Visibility = Visibility.Visible;
    }

    private void CancelEdit()
    {
        if (!_isEditing)
        {
            return;
        }

        _isEditing = false;
        Editor.Visibility = Visibility.Collapsed;
        Display.Visibility = Visibility.Visible;
    }
}
