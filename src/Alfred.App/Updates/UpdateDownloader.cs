using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;

namespace Alfred.App.Updates;

public static class UpdateDownloader
{
    private const int BufferSize = 81920;

    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Alfred",
        "updates");

    public static async Task<string> DownloadAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(release);

        string releaseFolder = Path.Combine(Folder, Sanitize(release.Tag));
        Directory.CreateDirectory(releaseFolder);
        string zipPath = Path.Combine(releaseFolder, ResolveFileName(release));

        try
        {
            await WriteAssetAsync(release, zipPath, progress, cancellationToken);
            VerifyArchive(zipPath);
            progress?.Report(1d);
            return zipPath;
        }
        catch (Exception failure) when (failure is HttpRequestException or IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            DeleteQuietly(zipPath);
            throw;
        }
    }

    private static async Task WriteAssetAsync(ReleaseInfo release, string zipPath, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, release.DownloadUrl);
        using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var reporter = new DownloadProgress(response.Content.Headers.ContentLength ?? release.SizeBytes, progress);
        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await CopyAsync(source, target, reporter, cancellationToken);
    }

    private static async Task CopyAsync(Stream source, Stream target, DownloadProgress reporter, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        long copiedBytes = 0;

        while (true)
        {
            int read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copiedBytes += read;
            reporter.Report(copiedBytes);
        }
    }

    private static void VerifyArchive(string zipPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count == 0)
        {
            throw new InvalidDataException($"The update downloaded to '{zipPath}' is not a usable archive.");
        }
    }

    private static string ResolveFileName(ReleaseInfo release)
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
        var sanitized = new StringBuilder(value.Length);
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

    private sealed record DownloadProgress(long TotalBytes, IProgress<double>? Listener)
    {
        public void Report(long copiedBytes)
        {
            if (Listener is null || TotalBytes <= 0)
            {
                return;
            }

            Listener.Report(Math.Clamp((double)copiedBytes / TotalBytes, 0d, 1d));
        }
    }
}
