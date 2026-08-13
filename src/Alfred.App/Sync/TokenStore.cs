using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Alfred.App.Sync;

internal static class TokenStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Alfred");

    private static readonly string FilePath = Path.Combine(Folder, "microsoft-account.bin");

    public static TokenSet? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            byte[] plain = TokenProtection.Unprotect(File.ReadAllBytes(FilePath));
            return JsonSerializer.Deserialize<TokenSet>(plain);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            return null;
        }
    }

    public static void Save(TokenSet tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        Directory.CreateDirectory(Folder);
        File.WriteAllBytes(FilePath, TokenProtection.Protect(JsonSerializer.SerializeToUtf8Bytes(tokens)));
    }

    public static void Clear()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        File.Delete(FilePath);
    }
}
