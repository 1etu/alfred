using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Alfred.UIKit.Controls;

public static class Adaptive
{
    public static readonly DependencyProperty CenterProperty = DependencyProperty.RegisterAttached(
        "Center", typeof(bool), typeof(Adaptive), new PropertyMetadata(false, OnCenterChanged));

    public static void SetCenter(DependencyObject target, bool value) => target.SetValue(CenterProperty, value);

    public static bool GetCenter(DependencyObject target) => (bool)target.GetValue(CenterProperty);

    private static void OnCenterChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not FrameworkElement element || e.NewValue is not true)
        {
            return;
        }

        element.Loaded += (_, _) =>
        {
            ScrollViewer? host = FindHost(element);
            if (host is null)
            {
                return;
            }

            Apply(element, host.ActualWidth);
            host.SizeChanged += (_, args) => Apply(element, args.NewSize.Width);
        };
    }

    private static ScrollViewer? FindHost(FrameworkElement element)
    {
        DependencyObject? current = element;

        while (current is not null)
        {
            if (current is ScrollViewer viewer)
            {
                return viewer;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void Apply(FrameworkElement element, double hostWidth)
    {
        if (hostWidth <= 0)
        {
            return;
        }

        element.MaxWidth = Math.Clamp(hostWidth * 0.66, 680, 1120);
    }
}
