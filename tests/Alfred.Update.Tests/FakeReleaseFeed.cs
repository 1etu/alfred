using System.IO;
using Alfred.Update.Checking;

namespace Alfred.Update.Tests;

internal sealed class FakeReleaseFeed : IReleaseFeed
{
    public string Payload { get; set; } = "[]";

    public byte[] AssetBytes { get; set; } = [];

    public Exception? ListingFailure { get; set; }

    public int ListingReads { get; private set; }

    public Task<string> ReadReleasesAsync(CancellationToken cancellationToken)
    {
        ListingReads++;
        return ListingFailure is null
            ? Task.FromResult(Payload)
            : Task.FromException<string>(ListingFailure);
    }

    public Task<FeedAsset> OpenAssetAsync(Release release, CancellationToken cancellationToken) =>
        Task.FromResult(new FeedAsset(new MemoryStream(AssetBytes), AssetBytes.Length));
}
