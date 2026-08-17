using System.Diagnostics;
using System.Net.Http;
using Alfred.App.Preferences;
using Alfred.App.Sync;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.Theme;
using Alfred.Theme.Catalog;
using Alfred.Theme.Defaults;
using Alfred.UIKit;
using Alfred.UIKit.Controls;
using Alfred.UIKit.Input;
using Alfred.Update;

namespace Alfred.App.ViewModels;

public sealed class SettingsViewModel : Observable
{
    private const string PortalUrl = "https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade";

    private readonly UserPreferences _preferences;
    private readonly Vault _vault;

    public SettingsViewModel(UserPreferences preferences, ShortcutRegistry shortcuts, Vault vault)
    {
        _preferences = preferences;
        _vault = vault;
        Shortcuts = shortcuts;
        Account = new MicrosoftAccount();
        Updates = new UpdateService();
    }

    public ShortcutRegistry Shortcuts { get; }

    internal MicrosoftAccount Account { get; }

    public UpdateService Updates { get; }

    public bool IsSystemTheme
    {
        get => _preferences.Theme == ThemeCatalog.SystemSelection;
        set => SelectTheme(ThemeCatalog.SystemSelection, value);
    }

    public bool IsLightTheme
    {
        get => _preferences.Theme == DefaultThemes.Light.Name;
        set => SelectTheme(DefaultThemes.Light.Name, value);
    }

    public bool IsDarkTheme
    {
        get => _preferences.Theme == DefaultThemes.Dark.Name;
        set => SelectTheme(DefaultThemes.Dark.Name, value);
    }

    public bool IsGlassEnabled
    {
        get => _preferences.IsGlassEnabled;
        set
        {
            if (_preferences.IsGlassEnabled == value)
            {
                return;
            }

            _preferences.IsGlassEnabled = value;
            PreferencesStore.Save(_preferences);
            Raise();
        }
    }

    public string DefaultCurrency => _preferences.DefaultCurrency;

    public bool IsLiraDefault
    {
        get => _preferences.DefaultCurrency == Currencies.Lira.Code;
        set => SelectCurrency(Currencies.Lira.Code, value);
    }

    public bool IsDollarDefault
    {
        get => _preferences.DefaultCurrency == Currencies.Dollar.Code;
        set => SelectCurrency(Currencies.Dollar.Code, value);
    }

    public bool IsEuroDefault
    {
        get => _preferences.DefaultCurrency == Currencies.Euro.Code;
        set => SelectCurrency(Currencies.Euro.Code, value);
    }

    public bool IsPoundDefault
    {
        get => _preferences.DefaultCurrency == Currencies.Pound.Code;
        set => SelectCurrency(Currencies.Pound.Code, value);
    }

    private void SelectCurrency(string code, bool isSelected)
    {
        if (!isSelected || _preferences.DefaultCurrency == code)
        {
            return;
        }

        _preferences.DefaultCurrency = code;
        PreferencesStore.Save(_preferences);

        Raise(nameof(DefaultCurrency));
        Raise(nameof(IsLiraDefault));
        Raise(nameof(IsDollarDefault));
        Raise(nameof(IsEuroDefault));
        Raise(nameof(IsPoundDefault));
    }

    public bool ShowCounts
    {
        get => _preferences.ShowCounts;
        set
        {
            if (_preferences.ShowCounts == value)
            {
                return;
            }

            _preferences.ShowCounts = value;
            PreferencesStore.Save(_preferences);
            Raise();
        }
    }

    public string? MicrosoftClientId
    {
        get => _preferences.MicrosoftClientId;
        set
        {
            string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            _preferences.MicrosoftClientId = trimmed;
            PreferencesStore.Save(_preferences);
            Raise(nameof(CanConnect));
            Raise(nameof(ClientIdHint));
        }
    }

    public bool CanConnect => Guid.TryParse(_preferences.MicrosoftClientId, out _);

    public string ClientIdHint => _preferences.MicrosoftClientId is null
        ? "Paste the Application (client) ID from your Azure app registration."
        : CanConnect ? "Looks valid." : "That is not a GUID.";

    public bool IsSignedIn => Account.IsSignedIn;

    public string AccountLabel => Account.AccountName ?? "Not connected";

    public string ConnectLabel => Account.IsSignedIn ? "Disconnect" : "Connect";

    public string SyncStatus
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public bool IsBusy
    {
        get;
        set => Set(ref field, value);
    }

    public string PreferencesPath => PreferencesStore.FilePath;

    public string Version => Updates.CurrentVersionText;

    public void OpenPortal() => Launch(PortalUrl);

    public async Task ConnectAsync()
    {
        if (!CanConnect)
        {
            SyncStatus = "Enter a valid ClientId first.";
            return;
        }

        if (Account.IsSignedIn)
        {
            Account.SignOut();
            SyncStatus = string.Empty;
            RefreshAccount();
            return;
        }

        try
        {
            IsBusy = true;
            SyncStatus = "Waiting for the browser…";
            await Account.SignInAsync(_preferences.MicrosoftClientId!, CancellationToken.None);
            SyncStatus = "Connected. Alfred can now write to your calendar.";
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException or OperationCanceledException)
        {
            SyncStatus = failure.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshAccount();
        }
    }

    public async Task SyncAsync()
    {
        if (!CanConnect || !Account.IsSignedIn)
        {
            SyncStatus = "Connect an account first.";
            return;
        }

        try
        {
            IsBusy = true;
            SyncStatus = "Syncing…";

            CalendarSync sync = new(token => Account.GetAccessTokenAsync(_preferences.MicrosoftClientId!, token));
            SyncResult result = await sync.SyncAsync(BuildItems(), CancellationToken.None);

            _preferences.LastSyncUtc = DateTimeOffset.UtcNow;
            PreferencesStore.Save(_preferences);

            SyncStatus = result.Created + result.Updated + result.Deleted == 0
                ? "Already up to date."
                : $"{result.Created} added · {result.Updated} updated · {result.Deleted} removed.";
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException)
        {
            SyncStatus = failure.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal void RefreshAccount()
    {
        Raise(nameof(IsSignedIn));
        Raise(nameof(AccountLabel));
        Raise(nameof(ConnectLabel));
    }

    private List<SyncItem> BuildItems()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly until = today.AddDays(90);
        List<SyncItem> items = [];

        foreach (LedgerEntry entry in _vault.Data.Entries)
        {
            foreach (DateOnly occurrence in entry.Schedule.Occurrences(today, until))
            {
                items.Add(new SyncItem(
                    $"{entry.Id:N}-{occurrence:yyyyMMdd}",
                    occurrence,
                    $"{entry.Title} · {MoneyFormat.WithSign(entry.Money, entry.Flow)}"));
            }
        }

        foreach (var reminder in _vault.Data.Reminders.Where(reminder => !reminder.Done && reminder.Due >= today && reminder.Due <= until))
        {
            items.Add(new SyncItem($"{reminder.Id:N}-{reminder.Due:yyyyMMdd}", reminder.Due, reminder.Title));
        }

        foreach (var todo in _vault.Data.Todos.Where(todo => !todo.Done && todo.Due is { } due && due >= today && due <= until))
        {
            items.Add(new SyncItem($"{todo.Id:N}-{todo.Due!.Value:yyyyMMdd}", todo.Due!.Value, todo.Title));
        }

        return items;
    }

    private static void Launch(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception failure) when (failure is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    private void SelectTheme(string selection, bool isSelected)
    {
        if (!isSelected || _preferences.Theme == selection)
        {
            return;
        }

        _preferences.Theme = selection;
        PreferencesStore.Save(_preferences);
        ThemeService.Apply(ThemeCatalog.Resolve(selection));

        Raise(nameof(IsSystemTheme));
        Raise(nameof(IsLightTheme));
        Raise(nameof(IsDarkTheme));
        Raise(nameof(CurrentTheme));
    }

    public string CurrentTheme => _preferences.Theme;
}
