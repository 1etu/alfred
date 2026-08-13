using System.IO;
using System.Text.Json;

namespace Alfred.App.Preferences;

public static class PreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Alfred");

    public static string FilePath { get; } = Path.Combine(Folder, "preferences.json");

    public static UserPreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new UserPreferences();
            }

            return JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(FilePath)) ?? new UserPreferences();
        }
        catch (Exception failure) when (failure is IOException or JsonException or UnauthorizedAccessException)
        {
            return new UserPreferences();
        }
    }

    public static void Save(UserPreferences preferences)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(preferences, SerializerOptions));
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
        }
    }
}
