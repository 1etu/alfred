using System.Windows;
using Alfred.App.Interop;
using Alfred.App.Theming;

namespace Alfred.App.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DesktopWindowManager.ApplyCornerPreference(this, WindowCornerPreference.Round);
        DesktopWindowManager.ApplyDarkTitleBar(this, Theme.IsDark);
    }
}
