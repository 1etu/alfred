using Alfred.App.Preferences;
using Alfred.App.Theming;

namespace Alfred.App.ViewModels;

public sealed class SettingsViewModel : Observable
{
    private readonly UserPreferences _preferences;

    public SettingsViewModel(UserPreferences preferences)
    {
        _preferences = preferences;
    }

    public bool IsSystemTheme
    {
        get => _preferences.Theme == nameof(ThemeVariant.System);
        set => SelectTheme(ThemeVariant.System, value);
    }

    public bool IsLightTheme
    {
        get => _preferences.Theme == nameof(ThemeVariant.Light);
        set => SelectTheme(ThemeVariant.Light, value);
    }

    public bool IsDarkTheme
    {
        get => _preferences.Theme == nameof(ThemeVariant.Dark);
        set => SelectTheme(ThemeVariant.Dark, value);
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
            Raise(nameof(IsGlassEnabled));
        }
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
            Raise(nameof(ShowCounts));
        }
    }

    public string PreferencesPath => PreferencesStore.FilePath;

    public string Version => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    public ThemeVariant CurrentTheme => Enum.TryParse(_preferences.Theme, out ThemeVariant variant)
        ? variant
        : ThemeVariant.System;

    private void SelectTheme(ThemeVariant variant, bool isSelected)
    {
        if (!isSelected || _preferences.Theme == variant.ToString())
        {
            return;
        }

        _preferences.Theme = variant.ToString();
        PreferencesStore.Save(_preferences);
        Theme.Apply(variant);

        Raise(nameof(IsSystemTheme));
        Raise(nameof(IsLightTheme));
        Raise(nameof(IsDarkTheme));
        Raise(nameof(CurrentTheme));
    }
}

