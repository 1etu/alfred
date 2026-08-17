using System.IO;
using System.IO.Compression;
using Alfred.Update.Fetching;
using Xunit;

namespace Alfred.Update.Tests.Fetching;

public sealed class ReleaseFetcherTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "alfred-tests", Guid.NewGuid().ToString("N"));

    private static Release SampleRelease => new(
        new Version(9, 9, 9),
        "v9.9.9",
        "notes",
        "https://example.test/v9.9.9/alfred-v9.9.9-win-x64.zip",
        0,
        DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task FetchesAndVerifiesTheArchive()
    {
        FakeReleaseFeed feed = new() { AssetBytes = ZipWith("Alfred.exe") };
        ReleaseFetcher fetcher = new(feed, _folder);
        List<FetchProgress> reports = [];

        string zipPath = await fetcher.FetchAsync(SampleRelease, new SynchronousProgress(reports), CancellationToken.None);

        Assert.True(File.Exists(zipPath));
        Assert.EndsWith(".zip", zipPath, StringComparison.Ordinal);
        Assert.True(reports.Count > 0);
        Assert.Equal(1d, reports[^1].Ratio);
    }

    [Fact]
    public async Task RejectsArchivesWithoutTheExecutable()
    {
        FakeReleaseFeed feed = new() { AssetBytes = ZipWith("readme.txt") };
        ReleaseFetcher fetcher = new(feed, _folder);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => fetcher.FetchAsync(SampleRelease, null, CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_folder, "*.zip", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task RejectsTornDownloads()
    {
        FakeReleaseFeed feed = new() { AssetBytes = [1, 2, 3, 4, 5] };
        ReleaseFetcher fetcher = new(feed, _folder);

        await Assert.ThrowsAnyAsync<InvalidDataException>(
            () => fetcher.FetchAsync(SampleRelease, null, CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_folder, "*.zip", SearchOption.AllDirectories));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static byte[] ZipWith(string entryName)
    {
        using MemoryStream buffer = new();

        using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using Stream entry = archive.CreateEntry(entryName).Open();
            entry.Write("payload"u8);
        }

        return buffer.ToArray();
    }

    private sealed class SynchronousProgress : IProgress<FetchProgress>
    {
        private readonly List<FetchProgress> _reports;

        public SynchronousProgress(List<FetchProgress> reports)
        {
            _reports = reports;
        }

        public void Report(FetchProgress value) => _reports.Add(value);
    }
}
