using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alfred.App.ViewModels;
using Alfred.Core.Ledger;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;
using Alfred.UIKit.Icons;
using Alfred.UIKit.Suggest;

namespace Alfred.App.Views;

public partial class CaptureOverlay : UserControl
{
    public sealed class TypeChoice : Observable
    {
        private readonly CaptureOverlay _owner;
        private bool _isSelected;

        public TypeChoice(CaptureKind kind, string name, string chipFamily, string iconKey, CaptureOverlay owner, bool selected)
        {
            Kind = kind;
            Name = name;
            ChipFamily = chipFamily;
            Icon = IconLibrary.Resolve(iconKey);
            _owner = owner;
            _isSelected = selected;
        }

        public CaptureKind Kind { get; }

        public string Name { get; }

        public string ChipFamily { get; }

        public ImageSource Icon { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (Set(ref _isSelected, value) && value)
                {
                    _owner.OnKindChanged(Kind);
                }
            }
        }
    }

    private static readonly string[] ChipOrder = ["amount", "date", "time", "brand"];

    private readonly ObservableCollection<TypeChoice> _choices = [];
    private readonly Dictionary<string, (ParseChip Chip, string Value)> _chips = [];
    private bool _needsTitle;

    public CaptureOverlay()
    {
        InitializeComponent();

        _choices.Add(new TypeChoice(CaptureKind.Todo, LocalizationService.Text(LocalizationKeys.CaptureKindTodo), "TODOs", "TodosIcon", this, true));
        _choices.Add(new TypeChoice(CaptureKind.Reminder, LocalizationService.Text(LocalizationKeys.CaptureKindReminder), "Reminders", "RemindersIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Expense, LocalizationService.Text(LocalizationKeys.CaptureKindExpense), "Payments", "PaymentsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Payment, LocalizationService.Text(LocalizationKeys.CaptureKindPayment), "Payments", "PaymentsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Income, LocalizationService.Text(LocalizationKeys.CaptureKindIncome), "Meals", "SubscriptionsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Wish, LocalizationService.Text(LocalizationKeys.CaptureKindWish), "Plans", "WishListIcon", this, false));
        TypeChips.ItemsSource = _choices;

        Bar.ParsesChanged += OnParsesChanged;
        OnKindChanged(CaptureKind.Todo);
    }

    public event EventHandler<CaptureRequest>? Captured;

    public void Open()
    {
        Visibility = Visibility.Visible;
        _needsTitle = false;
        Bar.Reset();
        Bar.FocusTitle();

        Hints.Hints =
        [
            new KeyHint("Enter", LocalizationService.Text(LocalizationKeys.CaptureHintSaves)),
            new KeyHint("Tab", LocalizationService.Text(LocalizationKeys.CaptureHintCompletes)),
            new KeyHint("Esc", LocalizationService.Text(LocalizationKeys.CaptureHintCloses)),
        ];

        Motion.FadeIn(this, 160);
        Motion.Pop(Card);
    }

    public void Close() => Visibility = Visibility.Collapsed;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is >= Key.D1 and <= Key.D6)
        {
            _choices[e.Key - Key.D1].IsSelected = true;
            e.Handled = true;
        }
    }

    internal void OnKindChanged(CaptureKind kind)
    {
        Bar.ParseAmount = kind is CaptureKind.Expense or CaptureKind.Payment or CaptureKind.Income or CaptureKind.Wish;
        Bar.ParseDate = kind != CaptureKind.Wish;
        Bar.TitleSource = kind is CaptureKind.Expense or CaptureKind.Payment or CaptureKind.Wish ? new BrandSource() : null;
        Bar.TitlePlaceholder = LocalizationService.Text(kind switch
        {
            CaptureKind.Todo => LocalizationKeys.CapturePlaceholderTodo,
            CaptureKind.Reminder => LocalizationKeys.CapturePlaceholderReminder,
            CaptureKind.Expense => LocalizationKeys.CapturePlaceholderExpense,
            CaptureKind.Payment => LocalizationKeys.CapturePlaceholderPayment,
            CaptureKind.Income => LocalizationKeys.CapturePlaceholderIncome,
            _ => LocalizationKeys.CapturePlaceholderWish,
        });
    }

    private void OnParsesChanged(object? sender, EventArgs e)
    {
        _needsTitle = false;
        RefreshChips();
        RefreshCommitLine();
    }

    private void RefreshChips()
    {
        SetChip(
            "amount",
            LocalizationService.Text(LocalizationKeys.ChipAmount),
            Bar.PickedAmount is decimal amount ? MoneyFormat.Compact(Money.Lira(amount)) : null,
            Bar.RejectAmount);

        SetChip(
            "date",
            LocalizationService.Text(LocalizationKeys.ChipDate),
            Bar.PickedDate is DateOnly date
                ? $"{Bar.PickedDateLabel} · {date.ToString("ddd, d MMM", LocalizationService.Current.Culture)}"
                : null,
            Bar.RejectDate);

        SetChip(
            "time",
            LocalizationService.Text(LocalizationKeys.ChipTime),
            Bar.PickedTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            Bar.RejectTime);

        SetChip(
            "brand",
            LocalizationService.Text(LocalizationKeys.ChipBrand),
            Bar.PickedBrandSlug is string slug ? BrandCatalog.Find(slug)?.Name : null,
            Bar.RejectBrand);

        Chips.Margin = _chips.Count == 0 ? new Thickness(3, 0, 0, 0) : new Thickness(3, 10, 0, 0);
    }

    private void SetChip(string type, string label, string? value, Action reject)
    {
        if (value is null)
        {
            if (_chips.Remove(type, out (ParseChip Chip, string Value) stale))
            {
                Chips.Children.Remove(stale.Chip);
            }

            return;
        }

        if (_chips.TryGetValue(type, out (ParseChip Chip, string Value) existing))
        {
            if (existing.Value == value)
            {
                return;
            }

            existing.Chip.Value = value;
            _chips[type] = (existing.Chip, value);
            return;
        }

        ParseChip chip = new()
        {
            Label = label,
            Value = value,
            Margin = new Thickness(0, 0, 6, 0),
        };
        chip.Dismissed += (_, _) => reject();

        Chips.Children.Insert(InsertIndexFor(type), chip);
        _chips[type] = (chip, value);
    }

    private int InsertIndexFor(string type)
    {
        int index = 0;

        foreach (string preceding in ChipOrder)
        {
            if (preceding == type)
            {
                break;
            }

            if (_chips.ContainsKey(preceding))
            {
                index++;
            }
        }

        return index;
    }

    private void RefreshCommitLine()
    {
        if (_needsTitle)
        {
            CommitLine.Text = LocalizationService.Text(LocalizationKeys.CaptureNeedsTitle);
            CommitLine.SetResourceReference(TextBlock.ForegroundProperty, "Overdue");
            return;
        }

        CaptureKind kind = _choices.First(choice => choice.IsSelected).Kind;
        string outcome = LocalizationService.Text(kind switch
        {
            CaptureKind.Todo => LocalizationKeys.CaptureOutcomeTodo,
            CaptureKind.Reminder => LocalizationKeys.CaptureOutcomeReminder,
            CaptureKind.Expense => LocalizationKeys.CaptureOutcomeExpense,
            CaptureKind.Payment => LocalizationKeys.CaptureOutcomePayment,
            CaptureKind.Income => LocalizationKeys.CaptureOutcomeIncome,
            _ => LocalizationKeys.CaptureOutcomeWish,
        });

        if (Bar.PickedDate is DateOnly date)
        {
            string due = LocalizationService.Text(
                LocalizationKeys.CaptureOutcomeDue,
                date.ToString("ddd, d MMM", LocalizationService.Current.Culture));
            outcome = outcome + " · " + due;
        }

        CommitLine.Text = outcome;
        CommitLine.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
    }

    private void OnScrimClick(object sender, MouseButtonEventArgs e) => Close();

    private void OnSubmitted(object? sender, EventArgs e)
    {
        if (Bar.Title.Length == 0)
        {
            _needsTitle = true;
            RefreshCommitLine();
            return;
        }

        CaptureKind kind = _choices.First(choice => choice.IsSelected).Kind;

        Captured?.Invoke(this, new CaptureRequest(
            kind,
            Bar.Title,
            Bar.PickedDate,
            Bar.PickedTime,
            Bar.PickedAmount,
            Bar.PickedBrandSlug));

        Close();
    }
}
