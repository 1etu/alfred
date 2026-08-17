using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Alfred.Update.Tests;

public sealed class UpdateServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "alfred-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CheckFindsAnAvailableRelease()
    {
        FakeReleaseFeed feed = new() { Payload = ReleaseFixtures.Listing(ReleaseFixtures.Entry("v99.0.0")) };
        UpdateService service = new(feed, _folder);

        await service.CheckAsync(UpdateChannel.Stable, CancellationToken.None);

        Assert.Equal(UpdateState.Available, service.State);
        Assert.NotNull(service.Available);
        Assert.Contains("v99.0.0", service.Message, StringComparison.Ordinal);
        Assert.NotNull(service.LastCheckedUtc);
    }

    [Fact]
    public async Task CheckReportsUpToDate()
    {
        UpdateService service = new(new FakeReleaseFeed(), _folder);

        await service.CheckAsync(UpdateChannel.Stable, CancellationToken.None);

        Assert.Equal(UpdateState.UpToDate, service.State);
        Assert.Contains(service.CurrentVersionText, service.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckNamesTheGitHubStatusOnFailure()
    {
        FakeReleaseFeed feed = new()
        {
            ListingFailure = new HttpRequestException("boom", null, HttpStatusCode.Forbidden),
        };
        UpdateService service = new(feed, _folder);

        await service.CheckAsync(UpdateChannel.Stable, CancellationToken.None);

        Assert.Equal(UpdateState.Failed, service.State);
        Assert.Contains("403", service.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckIfDueHonoursTheInterval()
    {
        FakeReleaseFeed feed = new();
        UpdateService service = new(feed, _folder) { LastCheckedUtc = DateTimeOffset.UtcNow };

        await service.CheckIfDueAsync(UpdateChannel.Stable, CancellationToken.None);

        Assert.Equal(0, feed.ListingReads);
    }

    [Fact]
    public async Task DownloadWithoutAReleaseFails()
    {
        UpdateService service = new(new FakeReleaseFeed(), _folder);

        await service.DownloadAsync(CancellationToken.None);

        Assert.Equal(UpdateState.Failed, service.State);
    }

    [Fact]
    public async Task DownloadEndsReadyWithFullProgress()
    {
        FakeReleaseFeed feed = new()
        {
            Payload = ReleaseFixtures.Listing(ReleaseFixtures.Entry("v99.0.0")),
            AssetBytes = ZipWithExecutable(),
        };
        UpdateService service = new(feed, _folder);

        await service.CheckAsync(UpdateChannel.Stable, CancellationToken.None);
        await service.DownloadAsync(CancellationToken.None);

        Assert.Equal(UpdateState.Ready, service.State);
        Assert.Equal(1d, service.Progress);
        Assert.Contains("v99.0.0", service.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static byte[] ZipWithExecutable()
    {
        using MemoryStream buffer = new();

        using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using Stream entry = archive.CreateEntry("Alfred.exe").Open();
            entry.Write("payload"u8);
        }

        return buffer.ToArray();
    }
}
