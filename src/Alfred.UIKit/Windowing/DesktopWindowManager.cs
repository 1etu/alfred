using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alfred.UIKit.Windowing;

public static partial class DesktopWindowManager
{
    private const int CornerPreferenceAttribute = 33;
    private const int ImmersiveDarkModeAttribute = 20;
    private const int SystemBackdropTypeAttribute = 38;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

    public static void ApplyCornerPreference(Window window, WindowCornerPreference preference)
    {
        IntPtr handle = ResolveHandle(window);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int value = (int)preference;
        DwmSetWindowAttribute(handle, CornerPreferenceAttribute, ref value, sizeof(int));
    }

    public static void ApplyDarkTitleBar(Window window, bool isDark)
    {
        IntPtr handle = ResolveHandle(window);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        int value = isDark ? 1 : 0;
        DwmSetWindowAttribute(handle, ImmersiveDarkModeAttribute, ref value, sizeof(int));
    }

    public static void ApplyBackdrop(Window window, WindowBackdrop backdrop)
    {
        IntPtr handle = ResolveHandle(window);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        bool isGlass = backdrop is not (WindowBackdrop.None or WindowBackdrop.Auto);

        if (PresentationSource.FromVisual(window) is HwndSource source && source.CompositionTarget is not null)
        {
            source.CompositionTarget.BackgroundColor = isGlass ? Colors.Transparent : Colors.Black;
        }

        int value = (int)backdrop;
        DwmSetWindowAttribute(handle, SystemBackdropTypeAttribute, ref value, sizeof(int));
    }

    private static IntPtr ResolveHandle(Window window) => new WindowInteropHelper(window).Handle;
}
