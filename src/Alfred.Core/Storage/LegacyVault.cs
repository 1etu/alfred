using System.Text.Json;

namespace Alfred.Core.Storage;

internal static class LegacyVault
{
    private const string ImportedSuffix = ".imported";

    internal static string PathBesides(string databasePath) => Path.ChangeExtension(databasePath, ".json");

    internal static VaultData? Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<VaultData>(File.ReadAllText(path));
        }
        catch (Exception failure) when (failure is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static void MarkImported(string path) =>
        File.Move(path, path + ImportedSuffix, overwrite: true);
}
