using System.Collections.Immutable;

namespace Flamenco.Packaging.Dpkg;

public class DpkgChangelogReaderTests
{
    [Fact]
    public async Task Test()
    {
        string changelog = """
            dotnet7 (7.0.118-0ubuntu1~24.04.1~ppa1) noble; urgency=medium
            
              * Initial release for Ubuntu 24.04 LTS (Noble Numbat):
                - debian/control: Switch to libicu74.
                - debian/patches/add-ubuntu-noble-numbat-runtime-identifier.patch
            
             -- Dominik Viererbe <dominik.viererbe@canonical.com>  Fri, 05 Apr 2024 15:47:39 +0300
            """;

        using var changelogReader = new DpkgChangelogReader(new StringReader(changelog));

        var readEntryResult = await changelogReader.ReadChangelogEntryAsync();
        Assert.True(readEntryResult.IsSuccess);
        Assert.True(readEntryResult.HasValue);
        Assert.True(readEntryResult.Value.HasValue);
        var entry = readEntryResult.Value.Value;
        
        Assert.Equal(expected: "dotnet7", actual: entry.PackageName.Identifier);
        Assert.Equal(expected: "7.0.118-0ubuntu1~24.04.1~ppa1", actual: entry.Version.ToString());
        Assert.Equal(expected: new [] {"noble"}, actual: entry.Distributions.Select(dist => dist.ToString()));
        Assert.Single(entry.Metadata);
        Assert.True(entry.Metadata.TryGetValue(key: "urgency", out var value));
        Assert.Equal(expected: "medium", actual: value);
        Assert.Equal(expected: "medium", actual: entry.Urgency);
        Assert.Null(entry.BinaryOnly);
        
        Assert.Equal(
            expected: """
            
              * Initial release for Ubuntu 24.04 LTS (Noble Numbat):
                - debian/control: Switch to libicu74.
                - debian/patches/add-ubuntu-noble-numbat-runtime-identifier.patch
            
            
            """,
            actual: entry.Description);
        
        Assert.Equal(expected: "Dominik Viererbe", actual: entry.Maintainer.Name);
        Assert.Equal(expected: "dominik.viererbe@canonical.com", actual: entry.Maintainer.EmailAddress);
        Assert.Equal(
            expected: new DateTimeOffset(
                year: 2024, month: 4, day: 5, 
                hour: 15, minute: 47, second: 39, 
                offset: new TimeSpan(hours: 3, minutes: 0, seconds: 0)), 
            actual: entry.Date);
        
        readEntryResult = await changelogReader.ReadChangelogEntryAsync();
        Assert.True(readEntryResult.IsSuccess);
        Assert.Null(readEntryResult.Value);
    }

    [Fact]
    public async Task ToChangelogString_ReproducesTheParsedChangelog()
    {
        string changelog = """
            dotnet8 (8.0.131-8.0.31-0ubuntu1~24.04.1) noble-security; urgency=medium
            
              * New upstream release
              * SECURITY UPDATE: information disclosure
                - CVE-2026-58649: dotnet watch BrowserRefreshServer does not adequately
                  validate cross-origin WebSocket origins, potentially allowing IL and PDB
                  information disclosure.
            
             -- Nicolas Rincon <nicolas.ballesteros@canonical.com>  Wed, 02 Sep 2026 12:00:00 -0500
            """;

        using var changelogReader = new DpkgChangelogReader(new StringReader(changelog));

        var readEntryResult = await changelogReader.ReadChangelogEntryAsync();
        Assert.True(readEntryResult.IsSuccess);
        Assert.True(readEntryResult.Value.HasValue);

        // A changelog file ends with a line break, the raw string literal above does not.
        Assert.Equal(
            expected: changelog + '\n',
            actual: readEntryResult.Value.Value.ToChangelogString());
    }

    [Theory]
    [InlineData(5, 0, "+0500")]
    [InlineData(-5, 0, "-0500")]
    [InlineData(0, 0, "+0000")]
    [InlineData(5, 30, "+0530")]
    [InlineData(-10, 0, "-1000")]
    public void ToChangelogString_RendersOffsetWithoutColon(int hours, int minutes, string expectedOffset)
    {
        var entry = new ChangelogEntry(
            PackageName: DpkgName.Parse("dotnet8"),
            Version: DpkgVersion.Parse("8.0.131-8.0.31-0ubuntu1", formatProvider: null),
            Distributions: [DpkgSuite.Parse("noble")],
            Metadata: ImmutableDictionary<string, string>.Empty.Add("urgency", "medium"),
            Description: "\n  * Test.\n\n",
            Maintainer: new MaintainerInfo("Test", "test@example.com"),
            Date: new DateTimeOffset(2026, 9, 3, 12, 0, 0, new TimeSpan(hours, minutes, 0)));

        var str = entry.ToChangelogString();
        Assert.Contains($" {expectedOffset}\n", str);
        // The offset must not contain a colon, unlike .NET's default zzz formatting.
        var offsetWithColon = expectedOffset.Insert(3, ":");
        Assert.DoesNotContain(offsetWithColon, str);
    }
}