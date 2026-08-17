using System.Windows;
using System.Windows.Controls;
using Alfred.Update;
using Alfred.App.ViewModels;

namespace Alfred.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private SettingsViewModel? Model => DataContext as SettingsViewModel;

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.ConnectAsync();
        }
    }

    private async void OnSyncNow(object sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.SyncAsync();
        }
    }

    private void OnOpenPortal(object sender, RoutedEventArgs e) => Model?.OpenPortal();

    private async void OnCheckUpdates(object sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.Updates.CheckAsync(UpdateChannel.Stable, CancellationToken.None);
        }
    }

    private async void OnDownloadUpdate(object sender, RoutedEventArgs e)
    {
        if (Model is { } model)
        {
            await model.Updates.DownloadAsync(CancellationToken.None);
        }
    }

    private void OnInstallUpdate(object sender, RoutedEventArgs e)
    {
        if (Model is { } model && model.Updates.InstallAndRestart())
        {
            Application.Current.Shutdown();
        }
    }
}
