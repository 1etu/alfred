using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Alfred.App.Interop;
using Alfred.App.Theming;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class ShellWindow : Window
{
    private static readonly Thickness GlassFrame = new(-1);
    private static readonly Thickness SolidFrame = new(0);

    public ShellWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        DesktopWindowManager.ApplyCornerPreference(this, WindowCornerPreference.Round);
        ApplyChrome();

        if (DataContext is ShellViewModel shell)
        {
            shell.Settings.PropertyChanged += OnSettingsChanged;
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsGlassEnabled) or nameof(SettingsViewModel.CurrentTheme))
        {
            ApplyChrome();
        }
    }

    private void ApplyChrome()
    {
        bool isGlass = DataContext is ShellViewModel shell && shell.Settings.IsGlassEnabled;

        if (WindowChrome.GetWindowChrome(this) is WindowChrome chrome)
        {
            chrome.GlassFrameThickness = isGlass ? GlassFrame : SolidFrame;
        }

        if (isGlass)
        {
            Background = Brushes.Transparent;
        }
        else
        {
            SetResourceReference(BackgroundProperty, "ShellBackground");
        }

        DesktopWindowManager.ApplyDarkTitleBar(this, Theme.IsDark);
        DesktopWindowManager.ApplyBackdrop(this, isGlass ? WindowBackdrop.Acrylic : WindowBackdrop.None);
    }

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.IsSidebarExpanded = !shell.IsSidebarExpanded;
        }
    }
}
