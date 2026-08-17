using System.Windows;

namespace Alfred.App.Interop;

public static class Clipboards
{
    public static void Set(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            Clipboard.SetDataObject(text, copy: true);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
        }
    }
}
