using System.Globalization;

namespace Alfred.Update.Tests;

internal static class ReleaseFixtures
{
    public static string Listing(params string[] releases) => "[" + string.Join(',', releases) + "]";

    public static string Entry(
        string tag,
        bool draft = false,
        bool prerelease = false,
        string assetName = "alfred-win-x64.zip")
    {
        return string.Create(CultureInfo.InvariantCulture, $$"""
            {
              "tag_name": "{{tag}}",
              "draft": {{(draft ? "true" : "false")}},
              "prerelease": {{(prerelease ? "true" : "false")}},
              "body": "notes for {{tag}}",
              "published_at": "2026-08-01T00:00:00Z",
              "assets": [
                {
                  "name": "{{assetName}}",
                  "browser_download_url": "https://example.test/{{tag}}/{{assetName}}",
                  "size": 4096
                }
              ]
            }
            """);
    }
}
