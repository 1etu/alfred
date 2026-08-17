using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Alfred.App.Interop;
using Alfred.App.ViewModels;
using Alfred.Theme;

namespace Alfred.App.Views;

public partial class ShellWindow : Window
{
    private static readonly Thickness GlassFrame = new(-1);
    private static readonly Thickness SolidFrame = new(0);

    public ShellWindow()
    {
        InitializeComponent();
        Capture.Captured += OnCaptured;
    }

    public void OpenCapture() => Capture.Open();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        DesktopWindowManager.ApplyCornerPreference(this, WindowCornerPreference.Round);
        ApplyChrome();

        if (DataContext is ShellViewModel shell)
        {
            shell.Shortcuts.Attach(this);
            shell.Settings.PropertyChanged += OnSettingsChanged;
            shell.PropertyChanged += OnShellChanged;
        }
    }

    private void OnShellChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.CurrentContent))
        {
            return;
        }

        DoubleAnimation rise = new(16, 0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        DoubleAnimation fade = new(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        PageHost.RenderTransform.BeginAnimation(TranslateTransform.YProperty, rise);
        PageHost.BeginAnimation(OpacityProperty, fade);
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

        DesktopWindowManager.ApplyDarkTitleBar(this, ThemeService.IsDark);
        DesktopWindowManager.ApplyBackdrop(this, isGlass ? WindowBackdrop.Acrylic : WindowBackdrop.None);
    }

    private void OnCaptured(object? sender, CaptureRequest request)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.Capture(request);
        }
    }

    private void OnOpenCapture(object sender, RoutedEventArgs e) => Capture.Open();

    private void OnMore(object sender, RoutedEventArgs e)
    {
        if (MoreButton.ContextMenu is { } menu)
        {
            menu.PlacementTarget = MoreButton;
            menu.IsOpen = true;
        }
    }

    private void OnOpenTrash(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.ShowTrash();
        }
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.IsSettingsOpen = true;
        }
    }

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.IsSidebarExpanded = !shell.IsSidebarExpanded;
        }
    }
}
