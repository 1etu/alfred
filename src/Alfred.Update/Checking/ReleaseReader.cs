using System.Globalization;
using System.Text.Json;

namespace Alfred.Update.Checking;

public static class ReleaseReader
{
    private const string AssetSuffix = "-win-x64.zip";

    public static Release? FindNewest(string payload, UpdateChannel channel, Version installed)
    {
        ArgumentNullException.ThrowIfNull(installed);

        using JsonDocument document = JsonDocument.Parse(payload);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The release listing is not an array.");
        }

        Release? newest = null;

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (Read(element, channel, installed) is Release candidate &&
                (newest is null || candidate.Version > newest.Version))
            {
                newest = candidate;
            }
        }

        return newest;
    }

    private static Release? Read(JsonElement element, UpdateChannel channel, Version installed)
    {
        if (element.ValueKind != JsonValueKind.Object || ReadBoolean(element, "draft"))
        {
            return null;
        }

        if (ReadBoolean(element, "prerelease") && channel != UpdateChannel.Prerelease)
        {
            return null;
        }

        if (ReadString(element, "tag_name") is not string tag || !AppVersion.TryParse(tag, out Version? version))
        {
            return null;
        }

        if (version <= installed || ReadAsset(element) is not (string downloadUrl, long sizeBytes))
        {
            return null;
        }

        return new Release(
            version,
            tag,
            ReadString(element, "body") ?? string.Empty,
            downloadUrl,
            sizeBytes,
            ReadPublishedUtc(element));
    }

    private static (string DownloadUrl, long SizeBytes)? ReadAsset(JsonElement element)
    {
        if (!element.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            if (ReadString(asset, "name") is not string name ||
                !name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReadString(asset, "browser_download_url") is string downloadUrl)
            {
                return (downloadUrl, ReadSizeBytes(asset));
            }
        }

        return null;
    }

    private static long ReadSizeBytes(JsonElement asset)
        => asset.TryGetProperty("size", out JsonElement size) && size.ValueKind == JsonValueKind.Number
            ? size.GetInt64()
            : 0L;

    private static DateTimeOffset ReadPublishedUtc(JsonElement element)
    {
        if (ReadString(element, "published_at") is not string published)
        {
            return DateTimeOffset.MinValue;
        }

        return DateTimeOffset.TryParse(published, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTimeOffset publishedUtc)
            ? publishedUtc
            : DateTimeOffset.MinValue;
    }

    private static string? ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool ReadBoolean(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.True;
}
