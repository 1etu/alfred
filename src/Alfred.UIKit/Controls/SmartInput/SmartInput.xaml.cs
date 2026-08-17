using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Alfred.UIKit.Suggest;

namespace Alfred.UIKit.Controls;

public partial class SmartInput : UserControl
{
    private const double RowHeight = 34;
    private const double MaxOpenHeight = 210;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ISuggestionSource), typeof(SmartInput));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(SmartInput),
        new PropertyMetadata(null, (target, _) => ((SmartInput)target).RefreshPlaceholder()));

    public static readonly DependencyProperty MaxSuggestionsProperty = DependencyProperty.Register(
        nameof(MaxSuggestions), typeof(int), typeof(SmartInput), new PropertyMetadata(5));

    private readonly ObservableCollection<Suggestion> _suggestions = [];
    private bool _suppressSuggestions;
    private bool _isOpen;

    public SmartInput()
    {
        InitializeComponent();
        List.ItemsSource = _suggestions;
    }

    public event EventHandler<Suggestion>? Committed;

    public event EventHandler? Submitted;

    public ISuggestionSource? Source
    {
        get => (ISuggestionSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public int MaxSuggestions
    {
        get => (int)GetValue(MaxSuggestionsProperty);
        set => SetValue(MaxSuggestionsProperty, value);
    }

    public string Text
    {
        get => Input.Text;
        set
        {
            _suppressSuggestions = true;
            Input.Text = value;
            Input.CaretIndex = value.Length;
        }
    }

    public void FocusInput() => Input.Focus();

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshPlaceholder();

        if (_suppressSuggestions)
        {
            _suppressSuggestions = false;
            return;
        }

        Refresh();
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when !_isOpen:
                Refresh();
                e.Handled = true;
                break;

            case Key.Down:
                List.SelectedIndex = Math.Min(List.SelectedIndex + 1, _suggestions.Count - 1);
                List.ScrollIntoView(List.SelectedItem);
                e.Handled = true;
                break;

            case Key.Up when _isOpen:
                List.SelectedIndex = Math.Max(List.SelectedIndex - 1, 0);
                List.ScrollIntoView(List.SelectedItem);
                e.Handled = true;
                break;

            case Key.Enter or Key.Tab when _isOpen && List.SelectedItem is Suggestion chosen:
                Commit(chosen);
                e.Handled = e.Key == Key.Enter;
                break;

            case Key.Enter:
                Submitted?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;

            case Key.Escape when _isOpen:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void OnListClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject origin &&
            ItemsControl.ContainerFromElement(List, origin) is ListBoxItem item &&
            item.DataContext is Suggestion chosen)
        {
            Commit(chosen);
            e.Handled = true;
        }
    }

    private void OnInputLostFocus(object sender, KeyboardFocusChangedEventArgs e) => Close();

    private void Commit(Suggestion suggestion)
    {
        _suppressSuggestions = true;
        Input.Text = suggestion.Primary;
        Input.CaretIndex = Input.Text.Length;
        Close();
        Committed?.Invoke(this, suggestion);
    }

    private void Refresh()
    {
        _suggestions.Clear();

        if (Source is not null)
        {
            foreach (Suggestion suggestion in Source.Suggest(Input.Text, MaxSuggestions))
            {
                _suggestions.Add(suggestion);
            }
        }

        if (_suggestions.Count == 0 || !Input.IsKeyboardFocused)
        {
            Close();
            return;
        }

        List.SelectedIndex = 0;
        double target = Math.Min((_suggestions.Count * RowHeight) + 6, MaxOpenHeight);
        Animate(target, 160);
        _isOpen = true;
    }

    private void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        Animate(0, 120);
    }

    private void Animate(double target, int milliseconds)
    {
        DoubleAnimation animation = new()
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        List.BeginAnimation(MaxHeightProperty, animation);
    }

    private void RefreshPlaceholder()
    {
        PlaceholderText.Text = Placeholder ?? string.Empty;
        PlaceholderText.Visibility = Input.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
