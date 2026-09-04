using System.Text.RegularExpressions;
using Flamenco.Console.Commands;

namespace Flamenco.Console;

public class PrepareReleaseCommandTests
{
    #region Version parsing and derivation

    [Theory]
    [InlineData("8.0.130", 130)]
    [InlineData("9.0.19", 19)]
    [InlineData("10.0.112", 112)]
    [InlineData("8.0.131", 131)]
    public void TryGetPatch_ValidThreePartVersion_ReturnsPatch(string version, int expectedPatch)
    {
        Assert.True(PrepareReleaseCommand.TryGetPatch(version, out int patch));
        Assert.Equal(expectedPatch, patch);
    }

    [Theory]
    [InlineData("8.0")]
    [InlineData("8.0.abc")]
    [InlineData("")]
    [InlineData("8.0.130.1")]
    public void TryGetPatch_InvalidVersion_ReturnsFalse(string version)
    {
        Assert.False(PrepareReleaseCommand.TryGetPatch(version, out int _));
    }

    [Theory]
    [InlineData("8.0.130", "8.0.131")]
    [InlineData("9.0.19", "9.0.20")]
    [InlineData("10.0.112", "10.0.113")]
    public void TryReplacePatch_IncrementsPatch(string previous, string expected)
    {
        Assert.True(PrepareReleaseCommand.TryReplacePatch(previous, out _, out string result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("8.0.130", 131, "8.0.131")]
    [InlineData("8.0.100", 425, "8.0.425")]
    [InlineData("8.0.131", 132, "8.0.132")]
    public void TryReplacePatch_WithExplicitNewPatch_ReplacesPatch(string version, int newPatch, string expected)
    {
        Assert.True(PrepareReleaseCommand.TryReplacePatch(version, newPatch, out string result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("8.0.400", 4)]
    [InlineData("9.0.120", 1)]
    [InlineData("10.0.112", 1)]
    public void TryGetFeatureBand_BandFromPatch(string version, int expectedBand)
    {
        Assert.True(PrepareReleaseCommand.TryGetFeatureBand(version, out int band));
        Assert.Equal(expectedBand, band);
    }

    [Theory]
    [InlineData("8.0")]
    [InlineData("")]
    public void TryGetFeatureBand_InvalidVersion_ReturnsFalse(string version)
    {
        Assert.False(PrepareReleaseCommand.TryGetFeatureBand(version, out int _));
    }

    #endregion

    #region Revision counter reset

    [Theory]
    [InlineData("0ubuntu3", "0ubuntu1")]
    [InlineData("0ubuntu1~24.04.2", "0ubuntu1~24.04.1")]
    [InlineData("0ubuntu1~24.04.1~ppa2", "0ubuntu1~24.04.1~ppa1")]
    [InlineData("0ubuntu1", "0ubuntu1")]
    public void RevisionCounterPattern_ResetsTrailingCounters(string input, string expected)
    {
        var pattern = new Regex(@"\d+(?=$|~)", RegexOptions.Compiled);
        Assert.Equal(expected, pattern.Replace(input, "1"));
    }

    #endregion

    #region Wrap / column width

    [Fact]
    public void Wrap_Respects80ColumnLimit()
    {
        var lines = PrepareReleaseCommand.Wrap(
            "CVE-2026-00001: A long line that should be wrapped at the column limit when rendering.",
            "    - ",
            "      ");

        Assert.All(lines, line => Assert.True(line.Length <= 80, $"Line exceeded 80 columns: '{line}' ({line.Length})"));
    }

    [Fact]
    public void Wrap_LongUnbreakableWord_OverflowsSingleLine()
    {
        var lines = PrepareReleaseCommand.Wrap(
            "https://example.com/a/very/long/unbreakable/url/that/cannot/be/wrapped/at/all/xyz reachable remotely.",
            "    - ",
            "      ");

        Assert.Contains(lines, line => line.Length > 80);
    }

    [Fact]
    public void Wrap_EmptyInput_ReturnsEmptyList()
    {
        var lines = PrepareReleaseCommand.Wrap(string.Empty, "  * ", "    ");
        Assert.Empty(lines);
    }

    [Fact]
    public void Wrap_SingleShortWord_ReturnsOneLine()
    {
        var lines = PrepareReleaseCommand.Wrap("CVE-2026-00001", "    - ", "      ");
        Assert.Single(lines);
        Assert.Equal("    - CVE-2026-00001", lines[0]);
    }

    [Fact]
    public void Wrap_PreservesPrefixOnContinuationLines()
    {
        var lines = PrepareReleaseCommand.Wrap(
            "word1 word2 word3",
            "  * ",
            "    ");

        Assert.Equal("  * word1 word2 word3", lines[0]);
    }

    [Fact]
    public void Wrap_MultipleWords_WrapAtWidth()
    {
        var longText = "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi " +
                       "omicron pi rho sigma tau upsilon phi chi psi omega";
        var lines = PrepareReleaseCommand.Wrap(longText, "  * ", "    ");
        Assert.True(lines.Count > 1);
        Assert.All(lines, line => Assert.True(line.Length <= 80, $"Line exceeded 80 cols: '{line}' ({line.Length})"));
    }

    #endregion

    #region Linux platform filter

    [Theory]
    [InlineData(new string[] { "all" }, true)]
    [InlineData(new string[] { "linux" }, true)]
    [InlineData(new string[] { "windows" }, false)]
    [InlineData(new string[] { "windows", "linux" }, true)]
    [InlineData(new string[] { }, false)]
    public void AffectsLinux_PlatformFilters(string[] platforms, bool expected)
    {
        var disclosure = new PrepareReleaseCommand.Disclosure(
            Id: "CVE-TEST",
            Platforms: platforms,
            Description: ["x"],
            Cna: null);

        Assert.Equal(expected, PrepareReleaseCommand.AffectsLinux(disclosure));
    }

    #endregion

    #region CVE data lookup

    [Fact]
    public void FindFixedVersion_PrefersDotNetRuntime_FallsBackToAspNetCore()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", release, "8.0.30"),
                new PrepareReleaseCommand.Fix("dotnet-aspnetcore", release, "8.0.31")
            ],
            Packages: [],
            ReleaseCves: []);

        var runtime = PrepareReleaseCommand.FindFixedVersion(document, "dotnet-runtime", release, null);
        var fallback = PrepareReleaseCommand.FindFixedVersion(document, "dotnet-aspnetcore", release, null);

        Assert.Equal("8.0.30", runtime);
        Assert.Equal("8.0.31", fallback);
    }

    [Fact]
    public void FindFixedVersion_FeatureBandFiltering_SelectsCorrectBand()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products: [],
            Packages:
            [
                new PrepareReleaseCommand.Fix("dotnet-sdk", release, "8.0.131"),
                new PrepareReleaseCommand.Fix("dotnet-sdk", release, "8.0.425")
            ],
            ReleaseCves: []);

        var band1 = PrepareReleaseCommand.FindFixedVersion(document, "dotnet-sdk", release, 1);
        var band4 = PrepareReleaseCommand.FindFixedVersion(document, "dotnet-sdk", release, 4);

        Assert.Equal("8.0.131", band1);
        Assert.Equal("8.0.425", band4);
    }

    [Fact]
    public void FindFixedVersion_AmbiguousCandidates_ReturnsNull()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products: [],
            Packages:
            [
                new PrepareReleaseCommand.Fix("dotnet-sdk", release, "8.0.131"),
                new PrepareReleaseCommand.Fix("dotnet-sdk", release, "8.0.132")
            ],
            ReleaseCves: []);

        Assert.Null(PrepareReleaseCommand.FindFixedVersion(document, "dotnet-sdk", release, 1));
    }

    [Fact]
    public void FindFixedVersion_MatchingNameInBothProductsAndPackages_Deduplicates()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", release, "8.0.31")
            ],
            Packages:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", release, "8.0.31")
            ],
            ReleaseCves: []);

        var result = PrepareReleaseCommand.FindFixedVersion(document, "dotnet-runtime", release, null);
        Assert.Equal("8.0.31", result);
    }

    [Fact]
    public void FindFixedVersion_NoMatchingName_ReturnsNull()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", release, "8.0.31")
            ],
            Packages: [],
            ReleaseCves: []);

        Assert.Null(PrepareReleaseCommand.FindFixedVersion(document, "dotnet-sdk", release, null));
    }

    [Fact]
    public void FindFixedVersion_WrongRelease_ReturnsNull()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", "10.0", "10.0.11")
            ],
            Packages: [],
            ReleaseCves: []);

        Assert.Null(PrepareReleaseCommand.FindFixedVersion(document, "dotnet-runtime", release, null));
    }

    [Fact]
    public void FindFixedVersion_EmptyFixed_ReturnsNull()
    {
        var release = "8.0";
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: [],
            Products:
            [
                new PrepareReleaseCommand.Fix("dotnet-runtime", release, "")
            ],
            Packages: [],
            ReleaseCves: []);

        Assert.Null(PrepareReleaseCommand.FindFixedVersion(document, "dotnet-runtime", release, null));
    }

    [Fact]
    public void FindFixedVersion_NullProductsAndPackages_ReturnsNull()
    {
        var document = new PrepareReleaseCommand.CveDocument(
            Disclosures: null,
            Products: null,
            Packages: null,
            ReleaseCves: null);

        Assert.Null(PrepareReleaseCommand.FindFixedVersion(document, "dotnet-runtime", "8.0", null));
    }

    #endregion

    #region Previous release detection (skips binary-only rebuilds)

    [Fact]
    public async Task ReadPreviousReleaseAsync_SkipsRebuildsWithSameUpstreamVersion()
    {
        string changelog = "dotnet10 (10.0.110-10.0.10-0ubuntu3) stonking; urgency=medium\n\n" +
                           "  * Rebuild 3.\n\n" +
                           " -- A B <a@b.c>  Tue, 01 Sep 2026 09:00:00 +0000\n\n" +
                           "dotnet10 (10.0.110-10.0.10-0ubuntu2) stonking; urgency=medium\n\n" +
                           "  * Rebuild 2.\n\n" +
                           " -- A B <a@b.c>  Mon, 31 Aug 2026 09:00:00 +0000\n\n" +
                           "dotnet10 (10.0.110-10.0.10-0ubuntu1) stonking; urgency=medium\n\n" +
                           "  * Rebuild 1.\n\n" +
                           " -- A B <a@b.c>  Sun, 30 Aug 2026 09:00:00 +0000\n\n" +
                           "dotnet10 (10.0.109-10.0.9-0ubuntu1) stonking; urgency=medium\n\n" +
                           "  * New upstream release.\n\n" +
                           " -- A B <a@b.c>  Mon, 10 Aug 2026 09:00:00 +0000\n";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, changelog);
        try
        {
            var previous = await PrepareReleaseCommand.ReadPreviousReleaseAsync(path, "10.0.110-10.0.10", CancellationToken.None);
            Assert.NotNull(previous);
            Assert.Equal("10.0.109-10.0.9", previous!.Value.Version.UpstreamVersion);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadPreviousReleaseAsync_NoDistinctRelease_ReturnsNull()
    {
        string changelog = "dotnet10 (10.0.110-10.0.10-0ubuntu1) stonking; urgency=medium\n\n" +
                           "  * Only entry.\n\n" +
                           " -- A B <a@b.c>  Tue, 01 Sep 2026 09:00:00 +0000\n";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, changelog);
        try
        {
            var previous = await PrepareReleaseCommand.ReadPreviousReleaseAsync(path, "10.0.110-10.0.10", CancellationToken.None);
            Assert.Null(previous);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadPreviousReleaseAsync_MissingFile_ReturnsNull()
    {
        var previous = await PrepareReleaseCommand.ReadPreviousReleaseAsync(
            "/tmp/does-not-exist-changelog",
            "10.0.110-10.0.10",
            CancellationToken.None);
        Assert.Null(previous);
    }

    [Fact]
    public async Task ReadPreviousReleaseAsync_ReturnsFirstDistinctEntry()
    {
        string changelog = "dotnet8 (8.0.131-8.0.31-0ubuntu1~24.04.1) noble-security; urgency=medium\n\n" +
                           "  * New upstream release.\n\n" +
                           " -- A B <a@b.c>  Tue, 01 Sep 2026 09:00:00 +0000\n\n" +
                           "dotnet8 (8.0.130-8.0.30-0ubuntu1~24.04.1) noble-security; urgency=medium\n\n" +
                           "  * Previous upstream release.\n\n" +
                           " -- A B <a@b.c>  Mon, 10 Aug 2026 09:00:00 +0000\n";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, changelog);
        try
        {
            var previous = await PrepareReleaseCommand.ReadPreviousReleaseAsync(path, "8.0.131-8.0.31", CancellationToken.None);
            Assert.NotNull(previous);
            Assert.Equal("8.0.130-8.0.30", previous!.Value.Version.UpstreamVersion);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadPreviousReleaseAsync_AllRebuildsSameVersion_ReturnsNull()
    {
        string changelog = "dotnet10 (10.0.110-10.0.10-0ubuntu3) stonking; urgency=medium\n\n" +
                           "  * Rebuild 3.\n\n" +
                           " -- A B <a@b.c>  Tue, 01 Sep 2026 09:00:00 +0000\n\n" +
                           "dotnet10 (10.0.110-10.0.10-0ubuntu2) stonking; urgency=medium\n\n" +
                           "  * Rebuild 2.\n\n" +
                           " -- A B <a@b.c>  Mon, 31 Aug 2026 09:00:00 +0000\n\n" +
                           "dotnet10 (10.0.110-10.0.10-0ubuntu1) stonking; urgency=medium\n\n" +
                           "  * Rebuild 1.\n\n" +
                           " -- A B <a@b.c>  Sun, 30 Aug 2026 09:00:00 +0000\n";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, changelog);
        try
        {
            var previous = await PrepareReleaseCommand.ReadPreviousReleaseAsync(path, "10.0.110-10.0.10", CancellationToken.None);
            Assert.Null(previous);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion
}