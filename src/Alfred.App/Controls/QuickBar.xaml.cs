using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Alfred.App.Suggest;
using Alfred.Core.Time;

namespace Alfred.App.Controls;

public partial class QuickBar : UserControl
{
    private const double RowHeight = 36;
    private const double MaxOpenHeight = 190;

    public static readonly DependencyProperty TitlePlaceholderProperty = DependencyProperty.Register(
        nameof(TitlePlaceholder), typeof(string), typeof(QuickBar),
        new PropertyMetadata(null, (target, _) => ((QuickBar)target).RefreshGhost()));

    public static readonly DependencyProperty TitleSourceProperty = DependencyProperty.Register(
        nameof(TitleSource), typeof(ISuggestionSource), typeof(QuickBar));

    public static readonly DependencyProperty ParseDateProperty = DependencyProperty.Register(
        nameof(ParseDate), typeof(bool), typeof(QuickBar), new PropertyMetadata(true));

    public static readonly DependencyProperty ParseAmountProperty = DependencyProperty.Register(
        nameof(ParseAmount), typeof(bool), typeof(QuickBar), new PropertyMetadata(false));

    private readonly ObservableCollection<Suggestion> _suggestions = [];
    private bool _isOpen;
    private bool _suppress;

    public QuickBar()
    {
        InitializeComponent();
        List.ItemsSource = _suggestions;
    }

    public event EventHandler? Submitted;

    public string? TitlePlaceholder
    {
        get => (string?)GetValue(TitlePlaceholderProperty);
        set => SetValue(TitlePlaceholderProperty, value);
    }

    public ISuggestionSource? TitleSource
    {
        get => (ISuggestionSource?)GetValue(TitleSourceProperty);
        set => SetValue(TitleSourceProperty, value);
    }

    public bool ParseDate
    {
        get => (bool)GetValue(ParseDateProperty);
        set => SetValue(ParseDateProperty, value);
    }

    public bool ParseAmount
    {
        get => (bool)GetValue(ParseAmountProperty);
        set => SetValue(ParseAmountProperty, value);
    }

    public string Title { get; private set; } = string.Empty;

    public DateOnly? PickedDate { get; private set; }

    public TimeOnly? PickedTime { get; private set; }

    public decimal? PickedAmount { get; private set; }

    public string? PickedBrandSlug { get; private set; }

    public void Reset()
    {
        _suppress = true;
        TitleField.Text = string.Empty;
        Title = string.Empty;
        PickedDate = null;
        PickedTime = null;
        PickedAmount = null;
        PickedBrandSlug = null;
        DateBadge.Visibility = Visibility.Collapsed;
        _suppress = false;
        Close();
        RefreshGhost();
    }

    public void FocusTitle() => TitleField.Focus();

    private void OnTitleChanged(object sender, TextChangedEventArgs e)
    {
        RefreshGhost();

        if (_suppress)
        {
            return;
        }

        PickedBrandSlug = null;
        Preview();

        List<Suggestion> offers = [];

        if (TitleSource is not null && TitleField.Text.Trim().Length >= 2)
        {
            offers.AddRange(TitleSource.Suggest(TitleField.Text.Trim(), 4));
        }

        Offer(offers);
    }

    private void Preview()
    {
        string text = TitleField.Text;

        if (ParseDate && DateHints.TryExtract(text, DateOnly.FromDateTime(DateTime.Now), out _, out DateOnly date, out string label))
        {
            DateBadgeText.Text = label + " · " + date.ToString("ddd, d MMM", CultureInfo.InvariantCulture);
            DateBadge.Visibility = Visibility.Visible;
        }
        else
        {
            DateBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void OnFieldKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when _isOpen:
                List.SelectedIndex = Math.Min(List.SelectedIndex + 1, _suggestions.Count - 1);
                e.Handled = true;
                break;

            case Key.Up when _isOpen:
                List.SelectedIndex = Math.Max(List.SelectedIndex - 1, 0);
                e.Handled = true;
                break;

            case Key.Enter or Key.Tab when _isOpen && List.SelectedItem is Suggestion chosen:
                Commit(chosen);
                e.Handled = e.Key == Key.Enter;
                break;

            case Key.Enter:
                Submit();
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

    private void Commit(Suggestion suggestion)
    {
        _suppress = true;
        PickedBrandSlug = (suggestion.Value as Brand)?.Slug ?? suggestion.BrandSlug;
        TitleField.Text = suggestion.Primary;
        TitleField.CaretIndex = TitleField.Text.Length;
        _suppress = false;
        RefreshGhost();
        Close();
    }

    private void Submit()
    {
        string text = TitleField.Text.Trim();
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        PickedDate = null;
        PickedTime = null;
        PickedAmount = null;

        if (ParseDate && DateHints.TryExtract(text, today, out string cleaned, out DateOnly date, out _))
        {
            text = cleaned;
            PickedDate = date;
        }

        System.Text.RegularExpressions.Match time = TimePattern.Match(text);
        if (time.Success &&
            TimeOnly.TryParseExact(time.Value.Trim(), ["H:mm", "HH:mm", "H.mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
        {
            PickedTime = parsedTime;
            text = text.Remove(time.Index, time.Length).Trim();
        }

        if (ParseAmount)
        {
            System.Text.RegularExpressions.Match amount = AmountPattern.Match(text);
            if (amount.Success &&
                decimal.TryParse(amount.Groups["v"].Value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out decimal parsedAmount))
            {
                PickedAmount = parsedAmount;
                text = text.Remove(amount.Index, amount.Length).Trim();
            }
        }

        Title = text.Trim().TrimEnd(',', '·', '-').Trim();
        Submitted?.Invoke(this, EventArgs.Empty);
    }

    private static readonly System.Text.RegularExpressions.Regex TimePattern = new(
        @"\b\d{1,2}[:.]\d{2}\b",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex AmountPattern = new(
        @"₺?\s?(?<v>\d{1,3}(?:\.\d{3})*(?:,\d{1,2})?|\d+(?:,\d{1,2})?)\s?(?:₺|tl)?\s*$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private void Offer(IReadOnlyList<Suggestion> suggestions)
    {
        _suggestions.Clear();

        foreach (Suggestion suggestion in suggestions)
        {
            _suggestions.Add(suggestion);
        }

        if (_suggestions.Count == 0)
        {
            Close();
            return;
        }

        List.SelectedIndex = 0;
        Animate(Math.Min((_suggestions.Count * RowHeight) + 8, MaxOpenHeight), 160);
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

    private void RefreshGhost()
    {
        TitleGhost.Text = TitlePlaceholder ?? string.Empty;
        TitleGhost.Visibility = TitleField.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
