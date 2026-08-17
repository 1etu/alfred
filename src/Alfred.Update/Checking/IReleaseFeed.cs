namespace Alfred.Update.Checking;

public interface IReleaseFeed
{
    Task<string> ReadReleasesAsync(CancellationToken cancellationToken);

    Task<FeedAsset> OpenAssetAsync(Release release, CancellationToken cancellationToken);
}
