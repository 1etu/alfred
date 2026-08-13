using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Alfred.App.Sync;
using Alfred.App.ViewModels;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;

namespace Alfred.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private ShellViewModel? Shell => DataContext as ShellViewModel;

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        if (Shell is not { } shell || shell.Settings.MicrosoftClientId is not { } clientId)
        {
            return;
        }

        SettingsViewModel settings = shell.Settings;

        if (settings.IsSignedIn)
        {
            settings.Account.SignOut();
            settings.RefreshAccount();
            settings.SyncStatus = string.Empty;
            return;
        }

        try
        {
            settings.IsBusy = true;
            settings.SyncStatus = "Waiting for the browser…";
            await settings.Account.SignInAsync(clientId, CancellationToken.None);
            settings.SyncStatus = "Connected.";
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException or OperationCanceledException)
        {
            settings.SyncStatus = failure.Message;
        }
        finally
        {
            settings.IsBusy = false;
            settings.RefreshAccount();
        }
    }

    private async void OnSyncNow(object sender, RoutedEventArgs e)
    {
        if (Shell is not { } shell || shell.Settings.MicrosoftClientId is not { } clientId)
        {
            return;
        }

        SettingsViewModel settings = shell.Settings;

        try
        {
            settings.IsBusy = true;
            settings.SyncStatus = "Syncing…";

            CalendarSync sync = new(ct => settings.Account.GetAccessTokenAsync(clientId, ct));
            SyncResult result = await sync.SyncAsync(BuildItems(shell.Vault), CancellationToken.None);

            settings.SyncStatus = $"Synced. {result.Created} new · {result.Updated} updated · {result.Deleted} removed.";
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException)
        {
            settings.SyncStatus = failure.Message;
        }
        finally
        {
            settings.IsBusy = false;
        }
    }

    private static List<SyncItem> BuildItems(Vault vault)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly until = today.AddDays(90);
        List<SyncItem> items = [];

        foreach (LedgerEntry entry in vault.Data.Entries)
        {
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(today, until))
            {
                string title = $"{entry.Title} · {MoneyFormat.WithSign(entry.Money, entry.Flow)}";
                items.Add(new SyncItem($"{entry.Id:N}-{occurrence:yyyyMMdd}", occurrence, title));
            }
        }

        foreach (var reminder in vault.Data.Reminders.Where(reminder => !reminder.Done && reminder.Due >= today && reminder.Due <= until))
        {
            items.Add(new SyncItem($"{reminder.Id:N}-{reminder.Due:yyyyMMdd}", reminder.Due, reminder.Title));
        }

        return items;
    }
}
