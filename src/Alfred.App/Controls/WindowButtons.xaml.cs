using System.Windows;
using System.Windows.Controls;

namespace Alfred.App.Controls;

public partial class WindowButtons : UserControl
{
    public WindowButtons()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        Window? window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        window.WindowState = WindowState.Minimized;
    }

    private void OnZoomClick(object sender, RoutedEventArgs e)
    {
        Window? window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
