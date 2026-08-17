using System.IO;

namespace Alfred.Update.Checking;

public sealed class FeedAsset : IAsyncDisposable
{
    private readonly IDisposable? _owner;

    public FeedAsset(Stream content, long totalBytes, IDisposable? owner = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
        TotalBytes = totalBytes;
        _owner = owner;
    }

    public Stream Content { get; }

    public long TotalBytes { get; }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        _owner?.Dispose();
    }
}
