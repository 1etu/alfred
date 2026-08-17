using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Alfred.App.ViewModels;
using Alfred.UIKit;
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
            Icon = Application.Current.Resources[iconKey] as System.Windows.Media.ImageSource;
            _owner = owner;
            _isSelected = selected;
        }

        public CaptureKind Kind { get; }

        public string Name { get; }

        public string ChipFamily { get; }

        public System.Windows.Media.ImageSource? Icon { get; }

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

    private readonly ObservableCollection<TypeChoice> _choices = [];

    public CaptureOverlay()
    {
        InitializeComponent();

        _choices.Add(new TypeChoice(CaptureKind.Todo, "Todo", "TODOs", "TodosIcon", this, true));
        _choices.Add(new TypeChoice(CaptureKind.Reminder, "Reminder", "Reminders", "RemindersIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Expense, "Expense", "Payments", "PaymentsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Payment, "Payment", "Payments", "PaymentsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Income, "Income", "Meals", "SubscriptionsIcon", this, false));
        _choices.Add(new TypeChoice(CaptureKind.Wish, "Wish", "Plans", "WishListIcon", this, false));
        TypeChips.ItemsSource = _choices;
    }

    public event EventHandler<CaptureRequest>? Captured;

    public void Open()
    {
        Visibility = Visibility.Visible;
        Bar.Reset();
        Bar.FocusTitle();

        DoubleAnimation fade = new(0, 1, TimeSpan.FromMilliseconds(160));
        BeginAnimation(OpacityProperty, fade);

        DoubleAnimation pop = new(0.96, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 },
        };

        CardScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pop);
        CardScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pop);
    }

    public void Close() => Visibility = Visibility.Collapsed;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    internal void OnKindChanged(CaptureKind kind)
    {
        Bar.ParseAmount = kind is CaptureKind.Expense or CaptureKind.Payment or CaptureKind.Income or CaptureKind.Wish;
        Bar.ParseDate = kind != CaptureKind.Wish;
        Bar.TitleSource = kind is CaptureKind.Expense or CaptureKind.Payment or CaptureKind.Wish ? new BrandSource() : null;
        Bar.TitlePlaceholder = kind switch
        {
            CaptureKind.Todo => "What needs doing?",
            CaptureKind.Reminder => "Remind me to…",
            CaptureKind.Expense => "What did you spend on?",
            CaptureKind.Payment => "What do you owe?",
            CaptureKind.Income => "What's coming in?",
            _ => "What do you want?",
        };
    }

    private void OnScrimClick(object sender, MouseButtonEventArgs e) => Close();

    private void OnSubmitted(object? sender, EventArgs e)
    {
        if (Bar.Title.Length == 0)
        {
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
