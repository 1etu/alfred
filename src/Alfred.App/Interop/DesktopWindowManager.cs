using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Alfred.App.Interop;

internal enum WindowCornerPreference
{
    Default = 0,
    DoNotRound = 1,
    Round = 2,
    RoundSmall = 3,
}

internal static partial class DesktopWindowManager
{
    private const int CornerPreferenceAttribute = 33;
    private const int ImmersiveDarkModeAttribute = 20;

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

    private static IntPtr ResolveHandle(Window window) => new WindowInteropHelper(window).Handle;
}
