using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Alfred.Update.Checking;

public sealed class GitHubReleaseFeed : IReleaseFeed
{
    private const string ReleasesEndpoint = "https://api.github.com/repos/1etu/alfred/releases?per_page=30";
    private const string GitHubApiVersion = "2022-11-28";

    private static readonly HttpClient Listing = CreateListingClient();
    private static readonly HttpClient Assets = new() { Timeout = Timeout.InfiniteTimeSpan };

    public async Task<string> ReadReleasesAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await Listing
            .GetAsync(new Uri(ReleasesEndpoint), cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeedAsset> OpenAssetAsync(Release release, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(release);

        HttpResponseMessage response = await Assets
            .GetAsync(new Uri(release.DownloadUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            response.Dispose();
            throw;
        }

        Stream content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return new FeedAsset(content, response.Content.Headers.ContentLength ?? release.SizeBytes, response);
    }

    private static HttpClient CreateListingClient()
    {
        HttpClient listing = new() { Timeout = TimeSpan.FromSeconds(10) };
        listing.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Alfred", AppVersion.Current.ToString()));
        listing.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        listing.DefaultRequestHeaders.Add("X-GitHub-Api-Version", GitHubApiVersion);
        return listing;
    }
}
