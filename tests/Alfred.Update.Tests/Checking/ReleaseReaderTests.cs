using System.Text.Json;
using Alfred.Update.Checking;
using Xunit;

namespace Alfred.Update.Tests.Checking;

public class ReleaseReaderTests
{
    private static readonly Version Installed = new(1, 0, 0);

    [Fact]
    public void SelectsTheNewestRelease()
    {
        string payload = ReleaseFixtures.Listing(
            ReleaseFixtures.Entry("v1.2.0"),
            ReleaseFixtures.Entry("v1.4.0"),
            ReleaseFixtures.Entry("v1.3.0"));

        Release? release = ReleaseReader.FindNewest(payload, UpdateChannel.Stable, Installed);

        Assert.NotNull(release);
        Assert.Equal("v1.4.0", release.Tag);
        Assert.Equal(new Version(1, 4, 0), release.Version);
        Assert.Equal("notes for v1.4.0", release.Notes);
        Assert.Equal(4096, release.SizeBytes);
    }

    [Fact]
    public void SkipsDraftsAndOlderReleases()
    {
        string payload = ReleaseFixtures.Listing(
            ReleaseFixtures.Entry("v9.0.0", draft: true),
            ReleaseFixtures.Entry("v0.9.0"));

        Assert.Null(ReleaseReader.FindNewest(payload, UpdateChannel.Stable, Installed));
    }

    [Fact]
    public void GatesPrereleasesByChannel()
    {
        string payload = ReleaseFixtures.Listing(ReleaseFixtures.Entry("v2.0.0", prerelease: true));

        Assert.Null(ReleaseReader.FindNewest(payload, UpdateChannel.Stable, Installed));
        Assert.NotNull(ReleaseReader.FindNewest(payload, UpdateChannel.Prerelease, Installed));
    }

    [Fact]
    public void RequiresTheWindowsAsset()
    {
        string payload = ReleaseFixtures.Listing(ReleaseFixtures.Entry("v2.0.0", assetName: "alfred-macos.zip"));

        Assert.Null(ReleaseReader.FindNewest(payload, UpdateChannel.Stable, Installed));
    }

    [Fact]
    public void ThrowsWhenTheListingIsNotAnArray()
    {
        Assert.Throws<JsonException>(() => ReleaseReader.FindNewest("{}", UpdateChannel.Stable, Installed));
        Assert.ThrowsAny<JsonException>(() => ReleaseReader.FindNewest("not json", UpdateChannel.Stable, Installed));
    }
}
