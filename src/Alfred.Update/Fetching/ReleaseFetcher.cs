using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Alfred.Update.Checking;

namespace Alfred.Update.Fetching;

public sealed class ReleaseFetcher
{
    private const int BufferSize = 81920;
    private const string ExecutableName = "Alfred.exe";

    private readonly IReleaseFeed _feed;
    private readonly string _folder;

    public ReleaseFetcher(IReleaseFeed feed, string folder)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        _feed = feed;
        _folder = folder;
    }

    public static string DefaultFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Alfred",
        "updates");

    public async Task<string> FetchAsync(Release release, IProgress<FetchProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(release);

        string releaseFolder = Path.Combine(_folder, Sanitize(release.Tag));
        Directory.CreateDirectory(releaseFolder);
        string zipPath = Path.Combine(releaseFolder, ResolveFileName(release));

        try
        {
            await WriteAssetAsync(release, zipPath, progress, cancellationToken).ConfigureAwait(false);
            VerifyContainsExecutable(zipPath);
            return zipPath;
        }
        catch (Exception failure) when (failure is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or OperationCanceledException)
        {
            DeleteQuietly(zipPath);
            throw;
        }
    }

    private async Task WriteAssetAsync(Release release, string zipPath, IProgress<FetchProgress>? progress, CancellationToken cancellationToken)
    {
        await using FeedAsset asset = await _feed.OpenAssetAsync(release, cancellationToken).ConfigureAwait(false);
        await using FileStream target = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        byte[] buffer = new byte[BufferSize];
        long copiedBytes = 0;

        while (true)
        {
            int read = await asset.Content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                progress?.Report(new FetchProgress(copiedBytes, copiedBytes));
                return;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            copiedBytes += read;
            progress?.Report(new FetchProgress(copiedBytes, asset.TotalBytes));
        }
    }

    private static void VerifyContainsExecutable(string zipPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(zipPath);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name.Equals(ExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new InvalidDataException($"The update package does not contain {ExecutableName}.");
    }

    private static string ResolveFileName(Release release)
    {
        if (!Uri.TryCreate(release.DownloadUrl, UriKind.Absolute, out Uri? downloadUri))
        {
            throw new InvalidDataException($"The release '{release.Tag}' has an unusable download address.");
        }

        string name = Sanitize(Path.GetFileName(downloadUri.LocalPath));
        return name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"alfred-{Sanitize(release.Tag)}-win-x64.zip";
    }

    private static string Sanitize(string value)
    {
        StringBuilder sanitized = new(value.Length);

        foreach (char character in value)
        {
            sanitized.Append(char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_');
        }

        return sanitized.Length == 0 ? "update" : sanitized.ToString();
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
        }
    }
}
