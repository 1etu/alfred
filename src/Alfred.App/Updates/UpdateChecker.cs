using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Alfred.App.Updates;

public sealed class UpdateChecker
{
    private const string ReleasesEndpoint = "https://api.github.com/repos/1etu/alfred/releases?per_page=30";
    private const string AssetSuffix = "-win-x64.zip";
    private const string GitHubApiVersion = "2022-11-28";

    private static readonly HttpClient Http = CreateHttp();

    public string? LastError { get; private set; }

    public DateTimeOffset? LastCheckedUtc { get; set; }

    public bool IsCheckDue(TimeSpan minimumInterval)
    {
        if (LastCheckedUtc is not DateTimeOffset lastChecked)
        {
            return true;
        }

        return DateTimeOffset.UtcNow - lastChecked >= minimumInterval;
    }

    public async Task<ReleaseInfo?> CheckAsync(UpdateChannel channel, CancellationToken cancellationToken)
    {
        LastError = null;

        string? payload = await ReadReleasesAsync(cancellationToken);
        LastCheckedUtc = DateTimeOffset.UtcNow;

        if (payload is null)
        {
            return null;
        }

        return SelectNewestRelease(payload, channel);
    }

    private async Task<string?> ReadReleasesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesEndpoint);
            using HttpResponseMessage response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LastError = $"GitHub replied {(int)response.StatusCode} when listing releases.";
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LastError = "The update check timed out.";
            return null;
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException or UriFormatException)
        {
            LastError = failure.Message;
            return null;
        }
    }

    private ReleaseInfo? SelectNewestRelease(string payload, UpdateChannel channel)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                LastError = "GitHub returned an unexpected release listing.";
                return null;
            }

            ReleaseInfo? newest = null;
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                if (ReadRelease(element, channel) is ReleaseInfo candidate && (newest is null || candidate.Version > newest.Version))
                {
                    newest = candidate;
                }
            }

            return newest;
        }
        catch (JsonException failure)
        {
            LastError = failure.Message;
            return null;
        }
    }

    private static ReleaseInfo? ReadRelease(JsonElement element, UpdateChannel channel)
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

        if (version <= AppVersion.Current || ReadAsset(element) is not ReleaseAsset asset)
        {
            return null;
        }

        return new ReleaseInfo(
            version,
            tag,
            ReadString(element, "body") ?? string.Empty,
            asset.DownloadUrl,
            asset.SizeBytes,
            ReadPublishedUtc(element));
    }

    private static ReleaseAsset? ReadAsset(JsonElement element)
    {
        if (!element.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            if (ReadString(asset, "name") is not string name || !name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReadString(asset, "browser_download_url") is not string downloadUrl)
            {
                continue;
            }

            return new ReleaseAsset(downloadUrl, ReadSizeBytes(asset));
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

    private static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Alfred", AppVersion.Current.ToString()));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", GitHubApiVersion);
        return http;
    }

    private sealed record ReleaseAsset(string DownloadUrl, long SizeBytes);
}
