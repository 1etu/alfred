using System.Globalization;
using System.Windows.Input;

namespace Alfred.App.Input;

public static class ShortcutGesture
{
    public static bool IsModifier(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or
        Key.LWin or Key.RWin or
        Key.System or Key.None;

    public static bool IsFunctionKey(Key key) => key is >= Key.F1 and <= Key.F24;

    public static bool IsReserved(Key key, ModifierKeys modifiers)
    {
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            return true;
        }

        if (modifiers.HasFlag(ModifierKeys.Alt) && key is Key.F4 or Key.Tab or Key.Space or Key.Escape)
        {
            return true;
        }

        return modifiers.HasFlag(ModifierKeys.Control) && key is Key.Escape;
    }

    public static bool IsBindable(Key key) =>
        !IsModifier(key) && key is not (Key.ImeProcessed or Key.DeadCharProcessed or Key.Cancel);

    public static bool IsComplete(Key key, ModifierKeys modifiers)
    {
        if (!IsBindable(key) || IsReserved(key, modifiers))
        {
            return false;
        }

        if (IsFunctionKey(key))
        {
            return true;
        }

        return modifiers.HasFlag(ModifierKeys.Control) || modifiers.HasFlag(ModifierKeys.Alt);
    }

    public static KeyGesture? TryCreate(Key key, ModifierKeys modifiers)
    {
        if (!IsComplete(key, modifiers))
        {
            return null;
        }

        try
        {
            return new KeyGesture(key, modifiers);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public static IReadOnlyList<string> Describe(ModifierKeys modifiers, Key key)
    {
        List<string> parts = [];

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        if (!IsModifier(key))
        {
            parts.Add(DescribeKey(key));
        }

        return parts;
    }

    public static string Serialize(KeyGesture gesture) =>
        string.Create(CultureInfo.InvariantCulture, $"{gesture.Modifiers}|{gesture.Key}");

    public static KeyGesture? Deserialize(string value)
    {
        string[] parts = value.Split('|');
        if (parts.Length != 2)
        {
            return null;
        }

        if (!Enum.TryParse(parts[0], out ModifierKeys modifiers) || !Enum.TryParse(parts[1], out Key key))
        {
            return null;
        }

        return TryCreate(key, modifiers);
    }

    private static string DescribeKey(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)(key - Key.D0)).ToString(CultureInfo.InvariantCulture),
        >= Key.NumPad0 and <= Key.NumPad9 => "Num " + (int)(key - Key.NumPad0),
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion => "/",
        Key.OemPlus => "=",
        Key.OemMinus => "-",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemPipe or Key.OemBackslash => "\\",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemTilde => "`",
        Key.Add => "Num +",
        Key.Subtract => "Num -",
        Key.Multiply => "Num *",
        Key.Divide => "Num /",
        Key.Decimal => "Num .",
        Key.Return => "Enter",
        Key.Escape => "Esc",
        Key.Back => "Backspace",
        Key.Delete => "Del",
        Key.Insert => "Ins",
        Key.Prior => "Page Up",
        Key.Next => "Page Down",
        Key.Space => "Space",
        Key.Tab => "Tab",
        Key.Left => "←",
        Key.Up => "↑",
        Key.Right => "→",
        Key.Down => "↓",
        _ => key.ToString(),
    };
}
