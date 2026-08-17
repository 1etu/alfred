using System.Windows.Media;

namespace Alfred.Theme.Defaults;

public static partial class DefaultThemes
{
    private static Color Rgb(uint value) =>
        Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);

    private static Color Argb(uint value) =>
        Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
}
