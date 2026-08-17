using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Alfred.UIKit.Input;

public static class SmoothScroll
{
    private const double PrecisionDelta = 120;
    private const double NotchDistance = 110;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(SmoothScroll), new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached(
        "Offset", typeof(double), typeof(SmoothScroll), new PropertyMetadata(0d, OnOffsetChanged));

    private static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target", typeof(double), typeof(SmoothScroll), new PropertyMetadata(double.NaN));

    public static void SetIsEnabled(DependencyObject target, bool value) => target.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject target) => (bool)target.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not ScrollViewer viewer)
        {
            return;
        }

        viewer.PreviewMouseWheel -= OnMouseWheel;

        if (e.NewValue is true)
        {
            viewer.PreviewMouseWheel += OnMouseWheel;
        }
    }

    private static void OnOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is ScrollViewer viewer)
        {
            viewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }

    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer viewer || viewer.ScrollableHeight <= 0)
        {
            return;
        }

        if (Math.Abs(e.Delta) < PrecisionDelta)
        {
            return;
        }

        double current = (double)viewer.GetValue(TargetProperty);
        if (double.IsNaN(current))
        {
            current = viewer.VerticalOffset;
        }

        double distance = NotchDistance * (e.Delta / PrecisionDelta);
        double target = Math.Clamp(current - distance, 0, viewer.ScrollableHeight);

        viewer.SetValue(TargetProperty, target);
        viewer.SetValue(OffsetProperty, viewer.VerticalOffset);

        DoubleAnimation animation = new()
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop,
        };

        animation.Completed += (_, _) => viewer.SetValue(OffsetProperty, target);

        viewer.BeginAnimation(OffsetProperty, animation);
        e.Handled = true;
    }
}
