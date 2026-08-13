namespace Alfred.App.Preferences;

public sealed class UserPreferences
{
    public string Theme { get; set; } = "System";

    public bool IsGlassEnabled { get; set; }

    public bool ShowCounts { get; set; } = true;

    public bool IsSidebarExpanded { get; set; } = true;

    public Dictionary<string, string> Shortcuts { get; set; } = [];

    public string? MicrosoftClientId { get; set; }
}
