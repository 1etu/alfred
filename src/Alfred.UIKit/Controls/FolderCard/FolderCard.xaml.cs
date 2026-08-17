using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Alfred.UIKit.Controls;

public partial class FolderCard : UserControl
{
    private const int PeekLimit = 3;

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(FolderCard));

    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
        nameof(Count), typeof(int), typeof(FolderCard),
        new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty ColorKeyProperty = DependencyProperty.Register(
        nameof(ColorKey), typeof(string), typeof(FolderCard),
        new PropertyMetadata(null, OnColorKeyChanged));

    public static readonly DependencyProperty PeekItemsProperty = DependencyProperty.Register(
        nameof(PeekItems), typeof(IReadOnlyList<string>), typeof(FolderCard),
        new PropertyMetadata(null, OnPeekItemsChanged));

    private static readonly DependencyPropertyKey TintBrushKey = DependencyProperty.RegisterReadOnly(
        nameof(TintBrush), typeof(Brush), typeof(FolderCard), new PropertyMetadata(null));

    public static readonly DependencyProperty TintBrushProperty = TintBrushKey.DependencyProperty;

    public FolderCard()
    {
        InitializeComponent();
        RefreshTint();
    }

    public event EventHandler? Opened;

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public string? ColorKey
    {
        get => (string?)GetValue(ColorKeyProperty);
        set => SetValue(ColorKeyProperty, value);
    }

    public IReadOnlyList<string>? PeekItems
    {
        get => (IReadOnlyList<string>?)GetValue(PeekItemsProperty);
        set => SetValue(PeekItemsProperty, value);
    }

    public Brush? TintBrush => (Brush?)GetValue(TintBrushProperty);

    private void OnOpen(object sender, RoutedEventArgs e) => Opened?.Invoke(this, EventArgs.Empty);

    private void OnPointerEnter(object sender, MouseEventArgs e) => AnimatePeek(isOpen: true);

    private void OnPointerLeave(object sender, MouseEventArgs e) => AnimatePeek(isOpen: false);

    private void AnimatePeek(bool isOpen)
    {
        double pocketScale = isOpen ? 0.94 : 1;

        if (!Motion.IsEnabled)
        {
            ApplyPeekInstantly(isOpen, pocketScale);
            return;
        }

        BackEase spring = new() { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
        int index = 0;

        foreach (UIElement card in Peek.Children)
        {
            if (card.RenderTransform is not TranslateTransform shift)
            {
                continue;
            }

            DoubleAnimation rise = new()
            {
                To = isOpen ? RiseFor(index) : 0,
                Duration = TimeSpan.FromMilliseconds(320),
                BeginTime = TimeSpan.FromMilliseconds(isOpen ? index * 50 : 0),
                EasingFunction = spring,
            };

            shift.BeginAnimation(TranslateTransform.YProperty, rise);
            index++;
        }

        DoubleAnimation settle = new()
        {
            To = pocketScale,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = spring,
        };

        PocketScale.BeginAnimation(ScaleTransform.ScaleYProperty, settle);
    }

    private void ApplyPeekInstantly(bool isOpen, double pocketScale)
    {
        int index = 0;

        foreach (UIElement card in Peek.Children)
        {
            if (card.RenderTransform is TranslateTransform shift)
            {
                shift.Y = isOpen ? RiseFor(index) : 0;
                index++;
            }
        }

        PocketScale.ScaleY = pocketScale;
    }

    private static double RiseFor(int index) => index == 1 ? -22 : -16;

    private void RefreshTint()
    {
        FolderColor color = FolderColors.Resolve(ColorKey);
        SetValue(TintBrushKey, Application.Current.Resources[color.BrushKey] as Brush);
    }

    private void RebuildPeek()
    {
        Peek.Children.Clear();

        if (PeekItems is not { Count: > 0 } items)
        {
            return;
        }

        foreach (string item in items.Take(PeekLimit))
        {
            Peek.Children.Add(BuildPeekCard(item));
        }
    }

    private static Border BuildPeekCard(string item)
    {
        return new Border
        {
            Width = 22,
            Height = 30,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(2, 0, 2, 0),
            Background = Application.Current.Resources[Alfred.Theme.ThemeKeys.CardBackground] as Brush,
            BorderBrush = Application.Current.Resources[Alfred.Theme.ThemeKeys.Hairline] as Brush,
            BorderThickness = new Thickness(1),
            RenderTransform = new TranslateTransform(0, 0),
            Child = new TextBlock
            {
                Text = item.Length == 0 ? string.Empty : item[..1].ToUpper(CultureInfo.CurrentCulture),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Application.Current.Resources[Alfred.Theme.ThemeKeys.TextSecondary] as Brush,
            },
        };
    }

    private static void OnCountChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((FolderCard)target).CountText.Text = ((int)e.NewValue).ToString(CultureInfo.CurrentCulture);

    private static void OnColorKeyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((FolderCard)target).RefreshTint();

    private static void OnPeekItemsChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) =>
        ((FolderCard)target).RebuildPeek();
}
