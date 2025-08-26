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

        var readEntryResult = await changelogReader.ReadChangelogEntryAsync().ConfigureAwait(false);
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
        
        readEntryResult = await changelogReader.ReadChangelogEntryAsync().ConfigureAwait(false);
        Assert.True(readEntryResult.IsSuccess);
        Assert.Null(readEntryResult.Value);
    }
}