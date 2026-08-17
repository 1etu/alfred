using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Alfred.UIKit;

public static class Motion
{
    private static readonly CubicEase EaseOut = Frozen(new CubicEase { EasingMode = EasingMode.EaseOut });
    private static readonly BackEase Spring = Frozen(new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 });

    public static bool IsEnabled => SystemParameters.ClientAreaAnimation;

    public static void FadeIn(UIElement element, int milliseconds = 140)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsEnabled)
        {
            element.Opacity = 1;
            return;
        }

        element.BeginAnimation(UIElement.OpacityProperty, Animation(0, 1, milliseconds, EaseOut));
    }

    public static void FadeOut(UIElement element, int milliseconds = 110)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsEnabled)
        {
            element.Opacity = 0;
            return;
        }

        element.BeginAnimation(UIElement.OpacityProperty, Animation(element.Opacity, 0, milliseconds, EaseOut));
    }

    public static void Rise(UIElement element, double fromOffset = 16, int milliseconds = 200)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsEnabled)
        {
            element.Opacity = 1;
            return;
        }

        TranslateTransform shift = new(0, fromOffset);
        element.RenderTransform = shift;
        shift.BeginAnimation(TranslateTransform.YProperty, Animation(fromOffset, 0, milliseconds, EaseOut));
        element.BeginAnimation(UIElement.OpacityProperty, Animation(0, 1, milliseconds, EaseOut));
    }

    public static void Pop(UIElement element, double fromScale = 0.96, int milliseconds = 220)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsEnabled)
        {
            element.Opacity = 1;
            return;
        }

        ScaleTransform scale = new(fromScale, fromScale);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = scale;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, Animation(fromScale, 1, milliseconds, Spring));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Animation(fromScale, 1, milliseconds, Spring));
    }

    public static void Shimmer(FrameworkElement element, int milliseconds = 400)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (!IsEnabled)
        {
            return;
        }

        TranslateTransform sweep = new(-1, 0);
        LinearGradientBrush mask = new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = sweep,
            GradientStops =
            [
                new GradientStop(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), 0),
                new GradientStop(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), 0.35),
                new GradientStop(Colors.White, 0.5),
                new GradientStop(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), 0.65),
                new GradientStop(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), 1),
            ],
        };

        DoubleAnimation slide = Animation(-1, 1, milliseconds, EaseOut);
        slide.FillBehavior = FillBehavior.Stop;
        slide.Completed += (_, _) => element.OpacityMask = null;

        element.OpacityMask = mask;
        sweep.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private static DoubleAnimation Animation(double from, double to, int milliseconds, IEasingFunction easing) => new()
    {
        From = from,
        To = to,
        Duration = TimeSpan.FromMilliseconds(milliseconds),
        EasingFunction = easing,
    };

    private static TEasing Frozen<TEasing>(TEasing easing)
        where TEasing : Freezable
    {
        easing.Freeze();
        return easing;
    }
}
