using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Alfred.Core.Ledger;
using Alfred.Core.Time;
using Alfred.UIKit.Suggest;

namespace Alfred.UIKit.Controls;

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
        nameof(ParseDate), typeof(bool), typeof(QuickBar),
        new PropertyMetadata(true, (target, _) => ((QuickBar)target).RefreshParses()));

    public static readonly DependencyProperty ParseAmountProperty = DependencyProperty.Register(
        nameof(ParseAmount), typeof(bool), typeof(QuickBar),
        new PropertyMetadata(false, (target, _) => ((QuickBar)target).RefreshParses()));

    private static readonly System.Text.RegularExpressions.Regex TimePattern = new(
        @"\b\d{1,2}[:.]\d{2}\b",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private sealed record TokenSpan(string Kind, int Start, int Length);

    private readonly ObservableCollection<Suggestion> _suggestions = [];
    private readonly List<TokenSpan> _spans = [];
    private readonly HashSet<string> _shownKinds = [];
    private (string Label, DateOnly Date)? _rejectedDate;
    private TimeOnly? _rejectedTime;
    private decimal? _rejectedAmount;
    private string _liveTitle = string.Empty;
    private bool _isOpen;
    private bool _suppress;

    public QuickBar()
    {
        InitializeComponent();
        List.ItemsSource = _suggestions;
        SizeChanged += (_, _) => RenderHighlights();
    }

    public event EventHandler? Submitted;

    public event EventHandler? ParsesChanged;

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

    public string? PickedDateLabel { get; private set; }

    public TimeOnly? PickedTime { get; private set; }

    public decimal? PickedAmount { get; private set; }

    public string? PickedCurrency { get; private set; }

    public string? PickedBrandSlug { get; private set; }

    public void Reset()
    {
        _suppress = true;
        TitleField.Text = string.Empty;
        _suppress = false;

        Title = string.Empty;
        PickedBrandSlug = null;
        _rejectedDate = null;
        _rejectedTime = null;
        _rejectedAmount = null;
        _shownKinds.Clear();
        Close();
        RefreshGhost();
        RefreshParses();
    }

    public void FocusTitle() => TitleField.Focus();

    public void RejectDate()
    {
        if (PickedDate is DateOnly date && PickedDateLabel is string label)
        {
            _rejectedDate = (label, date);
            RefreshParses();
        }
    }

    public void RejectTime()
    {
        if (PickedTime is TimeOnly time)
        {
            _rejectedTime = time;
            RefreshParses();
        }
    }

    public void RejectAmount()
    {
        if (PickedAmount is decimal amount)
        {
            _rejectedAmount = amount;
            RefreshParses();
        }
    }

    public void RejectBrand()
    {
        PickedBrandSlug = null;
        ParsesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshParses()
    {
        string original = TitleField.Text;
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        _spans.Clear();
        PickedDate = null;
        PickedDateLabel = null;
        PickedTime = null;
        PickedAmount = null;
        PickedCurrency = null;

        char[] masked = original.ToCharArray();

        if (ParseDate &&
            DateHints.Match(original, today) is DateMatch date &&
            _rejectedDate != (date.Label, date.Date))
        {
            PickedDate = date.Date;
            PickedDateLabel = date.Label;
            Consume(masked, "date", date.Start, date.Length);
        }

        System.Text.RegularExpressions.Match time = TimePattern.Match(new string(masked));
        if (time.Success &&
            TimeOnly.TryParseExact(time.Value.Trim(), ["H:mm", "HH:mm", "H.mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime) &&
            _rejectedTime != parsedTime)
        {
            PickedTime = parsedTime;
            Consume(masked, "time", time.Index, time.Length);
        }

        if (ParseAmount &&
            AmountHints.Match(new string(masked)) is AmountMatch amount &&
            _rejectedAmount != amount.Amount)
        {
            PickedAmount = amount.Amount;
            PickedCurrency = amount.CurrencyCode;
            Consume(masked, "amount", amount.Start, amount.Length);
        }

        _liveTitle = BuildTitle(original);
        RenderHighlights();
        ParsesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Consume(char[] masked, string kind, int start, int length)
    {
        _spans.Add(new TokenSpan(kind, start, length));

        for (int index = start; index < start + length; index++)
        {
            masked[index] = ' ';
        }
    }

    private string BuildTitle(string original)
    {
        string remainder = original;

        foreach (TokenSpan span in _spans.OrderByDescending(span => span.Start))
        {
            remainder = remainder.Remove(span.Start, span.Length);
        }

        return string.Join(' ', remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .TrimEnd(',', '·', '-')
            .Trim();
    }

    private void RenderHighlights()
    {
        HighlightLayer.Children.Clear();

        if (_spans.Count == 0)
        {
            _shownKinds.Clear();
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, PlaceHighlights);
    }

    private void PlaceHighlights()
    {
        HighlightLayer.Children.Clear();
        HashSet<string> current = [];

        foreach (TokenSpan span in _spans)
        {
            Rect leading = TitleField.GetRectFromCharacterIndex(span.Start);
            Rect trailing = TitleField.GetRectFromCharacterIndex(span.Start + span.Length);

            if (leading.IsEmpty || trailing.IsEmpty || trailing.Left <= leading.Left)
            {
                continue;
            }

            Border pill = new()
            {
                CornerRadius = new CornerRadius(5),
                Width = trailing.Left - leading.Left + 8,
                Height = leading.Height + 2,
            };

            pill.SetResourceReference(Border.BackgroundProperty, BrushKeyFor(span.Kind));
            Canvas.SetLeft(pill, Math.Max(leading.Left - 4, 0));
            Canvas.SetTop(pill, leading.Top - 1);
            HighlightLayer.Children.Add(pill);

            current.Add(span.Kind);

            if (!_shownKinds.Contains(span.Kind))
            {
                Motion.FadeIn(pill, 140);
            }
        }

        _shownKinds.Clear();
        _shownKinds.UnionWith(current);
    }

    private static string BrushKeyFor(string kind) => kind switch
    {
        "amount" => "StatGreenBack",
        "date" => "StatBlueBack",
        _ => "ChipRemindersBack",
    };

    private string? SpanKindAtCaret()
    {
        int caret = TitleField.CaretIndex;

        foreach (TokenSpan span in _spans)
        {
            if (caret >= span.Start && caret <= span.Start + span.Length)
            {
                return span.Kind;
            }
        }

        return null;
    }

    private void OnTitleChanged(object sender, TextChangedEventArgs e)
    {
        RefreshGhost();

        if (_suppress)
        {
            return;
        }

        PickedBrandSlug = null;
        RefreshParses();

        List<Suggestion> offers = [];

        if (TitleSource is not null && TitleField.Text.Trim().Length >= 2)
        {
            offers.AddRange(TitleSource.Suggest(TitleField.Text.Trim(), 4));
        }

        Offer(offers);
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

            case Key.Escape when SpanKindAtCaret() is string kind:
                Reject(kind);
                e.Handled = true;
                break;
        }
    }

    private void Reject(string kind)
    {
        switch (kind)
        {
            case "date":
                RejectDate();
                break;

            case "time":
                RejectTime();
                break;

            case "amount":
                RejectAmount();
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
        RefreshParses();
        Close();
    }

    private void Submit()
    {
        Title = _liveTitle;
        Submitted?.Invoke(this, EventArgs.Empty);
    }

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
