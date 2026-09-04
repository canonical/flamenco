using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flamenco.Distro.ReleaseInfo;
using Flamenco.Packaging;
using Flamenco.Packaging.Dpkg;
using Spectre.Console;

namespace Flamenco.Console.Commands;

public partial class PrepareReleaseCommand : Command
{
    /// <summary>The cve.json component that provides the .NET SDK half of the upstream version.</summary>
    private const string SdkComponentName = "dotnet-sdk";

    /// <summary>
    /// The cve.json component that provides the runtime half of the upstream version.
    /// </summary>
    /// <remarks>
    /// Microsoft reports the runtime version under either 'dotnet-runtime' or 'dotnet-aspnetcore'
    /// depending on the month, and both components share the same version for a given .NET release.
    /// 'dotnet-runtime' is preferred; 'dotnet-aspnetcore' is the fallback when the former is absent.
    /// </remarks>
    private const string PrimaryRuntimeComponentName = "dotnet-runtime";
    private const string FallbackRuntimeComponentName = "dotnet-aspnetcore";

    /// <summary>
    /// The maximum recommended line length of a Debian changelog entry.
    /// </summary>
    private const int ColumnLimit = 80;

    /// <summary>
    /// Matches the trailing counter of every revision segment, so that <c>0ubuntu3~24.04.2~ppa2</c>
    /// becomes <c>0ubuntu1~24.04.1~ppa1</c>. A new upstream release resets all of them.
    /// </summary>
    [GeneratedRegex(@"\d+(?=$|~)", RegexOptions.CultureInvariant)]
    private static partial Regex RevisionCounterPattern();

    /// <summary>Matches the <c>Name &lt;email&gt;</c> form that DEBEMAIL may carry, as supported by dch(1).</summary>
    [GeneratedRegex(@"^\s*(?<name>[^<]*?)\s*<(?<email>[^>]+)>\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex NameAndEmailPattern();

    private static readonly char[] UnrepresentableMaintainerCharacters = ['<', '>', '\n', '\r'];

    private static readonly JsonSerializerOptions CveFileJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Option<FileInfo> CveFileOption = new(
        name: "--cve-file")
    {
        Arity = ArgumentArity.ExactlyOne,
        DefaultValueFactory = _ => new FileInfo("cve.json"),
        Description = "Path to the Microsoft cve.json file.",
    };

    private static readonly Option<bool> ApplyOption = new(
        name: "--apply")
    {
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = _ => false,
        Description = "Write the generated changelog entries. Without this flag, changes are only previewed.",
    };

    public PrepareReleaseCommand() : base(
        name: "prepare-release",
        description: "Prepares the Debian changelog entries for a .NET servicing release from a " +
                     "Microsoft-provided cve.json file.")
    {
        Add(CommonOptions.SourceDirectoryOption);
        Add(CveFileOption);
        Add(ApplyOption);
        SetAction(InvokeAsync);
    }

    private static async Task<int> InvokeAsync(
        ParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        DirectoryInfo? sourceDirectory = parseResult.GetValue(CommonOptions.SourceDirectoryOption);
        FileInfo cveFile = parseResult.GetRequiredValue(CveFileOption);
        bool apply = parseResult.GetRequiredValue(ApplyOption);

        if (!EnvironmentVariables.TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(ref sourceDirectory))
        {
            return -1;
        }

        Log.Debug("Source Directory: " + sourceDirectory.FullName);
        Log.Debug("CVE File: " + cveFile.FullName);

        if (!await Program.IsPathAccessibleAsync(sourceDirectory.FullName, cancellationToken) ||
            !await Program.IsPathAccessibleAsync(cveFile.FullName, cancellationToken))
        {
            Log.Fatal("Aborting the release preparation, because some paths are not accessible.");
            return -1;
        }

        if (!TryResolveMaintainer(out MaintainerInfo maintainer)) return -1;
        Log.Info($"Maintainer: {maintainer.Name} <{maintainer.EmailAddress}>");

        if (!TryReadCveFile(cveFile, out CveDocument? cveDocument)) return -1;

        var sourceDirectoryInfoResult = SourceDirectoryInfo.FromDirectory(sourceDirectory);
        Log.Annotations(sourceDirectoryInfoResult);
        if (!sourceDirectoryInfoResult.IsSuccess)
        {
            Log.Fatal("Aborting the release preparation, because the source directory contains errors.");
            return -1;
        }

        var sourceDirectoryInfo = sourceDirectoryInfoResult.Value;

        // A servicing release is a single event, so every entry of this run shares one timestamp.
        var date = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(date.LocalDateTime);

        var buildTargets = sourceDirectoryInfo.BuildableTargets
            .OrderBy(target => target.PackageName, StringComparer.Ordinal)
            .ThenBy(target => target.SeriesName, StringComparer.Ordinal)
            .ToImmutableArray();

        int prepared = 0;
        int skipped = 0;
        int failed = 0;
        string? currentMajor = null;

        void Skip(BuildTarget target, string reason)
        {
            AnsiConsole.MarkupLine($"[yellow][[SKIP]][/] {target}: {Markup.Escape(reason)}");
            ++skipped;
        }

        foreach (var buildTarget in buildTargets)
        {
            // The .NET major version is the trailing number of the package name: dotnet8 -> "8.0".
            string majorVersion = new([.. buildTarget.PackageName.SkipWhile(c => !char.IsAsciiDigit(c))]);
            if (majorVersion.Length == 0 || !majorVersion.All(char.IsAsciiDigit))
            {
                Skip(buildTarget, $"the package name '{buildTarget.PackageName}' does not end in a .NET major version");
                continue;
            }

            string release = $"{majorVersion}.0";

            if (majorVersion != currentMajor)
            {
                currentMajor = majorVersion;
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule($"[bold].NET {release}[/]")
                {
                    Style = Style.Parse("blue"),
                    Justification = Justify.Left,
                });
            }

            var ubuntuRelease = UbuntuReleases.All
                .FirstOrDefault(candidate => candidate.Series.Identifier == buildTarget.SeriesName);

            if (ubuntuRelease is null)
            {
                Skip(buildTarget, $"'{buildTarget.SeriesName}' is not a known Ubuntu series");
                continue;
            }

            if (ubuntuRelease.EndOfStandardSupport < today)
            {
                Skip(buildTarget, $"the standard support of Ubuntu {ubuntuRelease.Version} ended on " +
                                  $"{ubuntuRelease.EndOfStandardSupport:yyyy-MM-dd}");
                continue;
            }

            if (cveDocument.ReleaseCves is null ||
                !cveDocument.ReleaseCves.TryGetValue(release, out string[]? releaseCveIds))
            {
                Skip(buildTarget, $"cve.json lists no disclosures for .NET {release}");
                continue;
            }

            // Ubuntu only ships Linux builds, so Windows-only disclosures are not applicable.
            var disclosures = (cveDocument.Disclosures ?? [])
                .Where(disclosure => releaseCveIds.Contains(disclosure.Id) && AffectsLinux(disclosure))
                .ToImmutableArray();

            if (disclosures.Length == 0)
            {
                Skip(buildTarget, $"none of the .NET {release} disclosures in cve.json affect Linux");
                continue;
            }

            // The previous entry provides the distribution, the metadata and the revision template.
            var previousEntryResult = await sourceDirectoryInfo
                .ReadFirstChangelogEntryAsync(buildTarget, cancellationToken)
                .ConfigureAwait(false);

            Log.Annotations(previousEntryResult);
            if (!previousEntryResult.IsSuccess)
            {
                Log.Error($"Preparing target {buildTarget} failed, because its changelog could not be read.");
                ++failed;
                continue;
            }

            var previousEntry = previousEntryResult.Value;

            // Binary-only rebuilds repeat the same upstream version with an incremented revision
            // (for example 0ubuntu3 -> 0ubuntu2 -> 0ubuntu1). The "previous release" is the most
            // recent entry whose upstream version differs from the current head, and it is the only
            // one that can be used for +1 version derivation.
            var previousReleaseResult = await ReadPreviousReleaseAsync(
                sourceDirectoryInfo.GetChangelogPath(buildTarget),
                previousEntry.Version.UpstreamVersion,
                cancellationToken).ConfigureAwait(false);

            string[] previousReleaseUpstream = previousReleaseResult is not null
                ? previousReleaseResult.Value.Version.UpstreamVersion.Split('-')
                : previousEntry.Version.UpstreamVersion.Split('-');

            // The upstream version is a compound of the SDK and the runtime version,
            // for example "8.0.131-8.0.31".
            if (previousReleaseUpstream.Length != 2 ||
                !TryGetFeatureBand(previousReleaseUpstream[0], out int featureBand))
            {
                Skip(buildTarget, $"the previous release '{previousReleaseResult?.Version ?? previousEntry.Version}' is not of the expected " +
                                  "'SDK-RUNTIME' upstream form");
                continue;
            }

            string? sdkVersion = FindFixedVersion(cveDocument, SdkComponentName, release, featureBand);
            string? runtimeVersion = FindFixedVersion(cveDocument, PrimaryRuntimeComponentName, release, featureBand: null);
            if (runtimeVersion is null)
            {
                runtimeVersion = FindFixedVersion(cveDocument, FallbackRuntimeComponentName, release, featureBand: null);
            }

            if (sdkVersion is null || runtimeVersion is null)
            {
                // Microsoft may omit a component from some monthly cve.json files. Derive both
                // halves from the previous release by incrementing each patch by one.
                if (previousReleaseUpstream.Length == 2 &&
                    TryReplacePatch(previousReleaseUpstream[0], out _, out sdkVersion) &&
                    TryReplacePatch(previousReleaseUpstream[1], out _, out runtimeVersion))
                {
                    AnsiConsole.MarkupLine(
                        $"[dim][[INFO]][/] {buildTarget.PackageName}:[bold]{buildTarget.SeriesName}[/]: " +
                        $"cve.json has no {SdkComponentName} or runtime fix; " +
                        $"derived SDK '{sdkVersion}' and runtime '{runtimeVersion}' from the previous release.");
                }

                if (sdkVersion is null || runtimeVersion is null)
                {
                    Skip(buildTarget, $"cve.json has no unambiguous {SdkComponentName} or runtime fix for .NET {release}, " +
                                      "and the versions cannot be derived from the previous changelog entry");
                    continue;
                }
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[dim][[INFO]][/] {buildTarget.PackageName}:[bold]{buildTarget.SeriesName}[/]: " +
                    $"using SDK version '{sdkVersion}' and runtime version '{runtimeVersion}' from cve.json.");
            }

            var versionBuilder = new StringBuilder();
            if (previousEntry.Version.Epoch is not null)
            {
                versionBuilder.Append(previousEntry.Version.Epoch).Append(':');
            }
            versionBuilder.Append(sdkVersion).Append('-').Append(runtimeVersion);
            if (previousEntry.Version.Revision is not null)
            {
                versionBuilder.Append('-').Append(
                    RevisionCounterPattern().Replace(previousEntry.Version.Revision, replacement: "1"));
            }

            var parseVersionResult = DpkgVersion.Parse(
                value: versionBuilder.ToString().AsSpan(),
                location: new Location());

            Log.Annotations(parseVersionResult);
            if (!parseVersionResult.IsSuccess)
            {
                Log.Error($"Preparing target {buildTarget} failed, because the version " +
                          $"'{versionBuilder}' derived from cve.json is malformed.");
                ++failed;
                continue;
            }

            var version = parseVersionResult.Value;

            if (version <= previousEntry.Version)
            {
                Skip(buildTarget, $"the changelog is already at version {previousEntry.Version}");
                continue;
            }

            WarnIfNotConsecutive(buildTarget, "SDK", previousEntry.Version.UpstreamVersion.Split('-')[0], sdkVersion!);
            WarnIfNotConsecutive(buildTarget, "runtime", previousEntry.Version.UpstreamVersion.Split('-')[1], runtimeVersion!);

            var entry = new ChangelogEntry(
                PackageName: previousEntry.PackageName,
                Version: version,
                Distributions: previousEntry.Distributions,
                // A new upstream release is never a binary-only rebuild.
                Metadata: previousEntry.Metadata.Remove(key: "binary-only"),
                Description: BuildDescription(disclosures),
                Maintainer: maintainer,
                Date: date);

            string changelogText = entry.ToChangelogString();
            AnsiConsole.MarkupLine(
                $"[green][[ADD]][/] {buildTarget}: {previousEntry.Version} -> {version} " +
                $"({disclosures.Length} CVE(s))");

            if (apply)
            {
                string path = sourceDirectoryInfo.GetChangelogPath(buildTarget);
                string existingChangelog = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(path, changelogText + '\n' + existingChangelog, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                System.Console.Out.WriteLine();
                System.Console.Out.Write(changelogText);
                System.Console.Out.WriteLine();
            }

            ++prepared;
        }

        AnsiConsole.MarkupLine(
            $"[[INFO]] Total Targets: {buildTargets.Length}; " +
            $"[green]Prepared: {prepared}[/]; " +
            $"[yellow]Skipped: {skipped}[/]; " +
            $"[red]Failed: {failed}[/]");

        if (failed > 0)
        {
            Log.Fatal("Preparing one or more changelog entries failed.");
            return 1;
        }

        if (prepared == 0)
        {
            Log.Warning("No changelog entries were prepared.");
        }
        else if (!apply)
        {
            AnsiConsole.MarkupLine(
                $"[dim]This was a dry run. Re-run with[/] [bold]'{ApplyOption.Name}'[/] " +
                $"[dim]to write these entries.[/]");
        }

        return 0;
    }

    /// <summary>
    /// Renders the description of a changelog entry for a servicing release.
    /// </summary>
    /// <remarks>
    /// The result starts and ends with a line break, which is the format
    /// <see cref="DpkgChangelogReader"/> reports and <see cref="ChangelogEntry.ToChangelogString"/> expects.
    /// </remarks>
    internal static string BuildDescription(ImmutableArray<Disclosure> disclosures)
    {
        var description = new StringBuilder();
        description.Append('\n');
        description.Append("  * New upstream release\n");

        foreach (var disclosure in disclosures)
        {
            string? impact = disclosure.Cna?.Impact?.ToLowerInvariant();
            string headline = string.IsNullOrWhiteSpace(impact)
                ? "SECURITY UPDATE:"
                : $"SECURITY UPDATE: {impact}";

            foreach (var line in Wrap(headline, firstPrefix: "  * ", prefix: "    "))
            {
                description.Append(line).Append('\n');
            }

            string summary = string.Join(separator: ' ', disclosure.Description ?? []);

            foreach (var line in Wrap($"{disclosure.Id}: {summary}", firstPrefix: "    - ", prefix: "      "))
            {
                description.Append(line).Append('\n');
            }
        }

        description.Append('\n');
        return description.ToString();
    }

    /// <summary>
    /// Greedily wraps <paramref name="text"/> to <see cref="ColumnLimit"/> - 1 characters.
    /// </summary>
    /// <remarks>
    /// Words that are longer than the available width overflow the line rather than being broken,
    /// which keeps identifiers and URLs intact.
    /// </remarks>
    internal static List<string> Wrap(string text, string firstPrefix, string prefix)
    {
        var lines = new List<string>();
        var line = new StringBuilder(firstPrefix);
        int prefixLength = firstPrefix.Length;

        foreach (var word in text.Split(separator: ' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > prefixLength)
            {
                if (line.Length + 1 + word.Length > ColumnLimit - 1)
                {
                    lines.Add(line.ToString());
                    line.Clear().Append(prefix);
                    prefixLength = prefix.Length;
                }
                else
                {
                    line.Append(' ');
                }
            }

            line.Append(word);
        }

        if (line.Length > prefixLength) lines.Add(line.ToString());

        return lines;
    }

    internal static bool AffectsLinux(Disclosure disclosure) =>
        (disclosure.Platforms ?? []).Any(platform =>
            platform.Equals("all", StringComparison.OrdinalIgnoreCase) ||
            platform.Equals("linux", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Looks up the version that fixes a component of a .NET release in the cve.json document.
    /// </summary>
    /// <param name="featureBand">
    /// If not <see langword="null"/>, only fixes of that .NET SDK feature band are considered. This is
    /// required for the SDK, because a single .NET release services multiple feature bands in parallel
    /// (for example 8.0.131 and 8.0.425).
    /// </param>
    /// <returns>
    /// The fixed version, or <see langword="null"/> if cve.json holds no or more than one candidate.
    /// </returns>
    internal static string? FindFixedVersion(CveDocument document, string name, string release, int? featureBand)
    {
        // Microsoft splits components over 'products' and 'packages' without a documented rule
        // for which goes where, so both are searched.
        var candidates = (document.Products ?? []).Concat(document.Packages ?? [])
            .Where(fix => fix.Name == name && fix.Release == release && !string.IsNullOrEmpty(fix.Fixed))
            .Select(fix => fix.Fixed!)
            .Where(fixedVersion => featureBand is null ||
                                   (TryGetFeatureBand(fixedVersion, out int band) && band == featureBand))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return candidates.Length == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Extracts the .NET SDK feature band of a version, so that <c>8.0.131</c> yields <c>1</c>.
    /// </summary>
    internal static bool TryGetFeatureBand(string version, out int featureBand)
    {
        if (TryGetPatch(version, out int patch))
        {
            featureBand = patch / 100;
            return true;
        }

        featureBand = default;
        return false;
    }

    /// <summary>
    /// Extracts the patch component from the last dotted segment of a version string.
    /// </summary>
    /// <remarks>
    /// Segments may include suffixes such as <c>~rc2</c>; only the leading numeric part is treated
    /// as the patch number.
    /// </remarks>
    internal static bool TryGetPatch(string version, out int patch)
    {
        string[] parts = version.Split('.');
        if (parts.Length == 3 && TryGetLeadingDigits(parts[2], out patch)) return true;

        patch = default;
        return false;
    }

    /// <summary>
    /// Replaces the patch component of a version string with <paramref name="newPatch"/>, preserving
    /// any suffix such as <c>~rc2</c>.
    /// </summary>
    internal static bool TryReplacePatch(string version, int newPatch, out string result)
    {
        string[] parts = version.Split('.');
        if (parts.Length == 3 && newPatch >= 0)
        {
            int suffixIndex = 0;
            while (suffixIndex < parts[2].Length && char.IsDigit(parts[2][suffixIndex])) suffixIndex++;
            string suffix = parts[2][suffixIndex..];
            parts[2] = newPatch.ToString(CultureInfo.InvariantCulture) + suffix;
            result = string.Join('.', parts);
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// Derives the next patch version from the previous changelog entry by incrementing the patch
    /// component by one.
    /// </summary>
    internal static bool TryReplacePatch(string version, out int previousPatch, out string result)
    {
        previousPatch = default;
        result = string.Empty;

        if (!TryGetPatch(version, out previousPatch)) return false;

        return TryReplacePatch(version, previousPatch + 1, out result);
    }

    private static bool TryGetLeadingDigits(string segment, out int value)
    {
        int length = 0;
        while (length < segment.Length && char.IsDigit(segment[length])) length++;
        if (length > 0 && int.TryParse(segment[..length], NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Reads the changelog file and returns the most recent entry whose upstream version differs
    /// from <paramref name="currentUpstreamVersion"/>.
    /// </summary>
    /// <remarks>
    /// Binary-only rebuilds leave the upstream version unchanged and only bump the revision. This
    /// method skips those rebuilds and returns the last actual upstream release.
    /// </remarks>
    internal static async Task<ChangelogEntry?> ReadPreviousReleaseAsync(
        string changelogPath,
        string currentUpstreamVersion,
        CancellationToken cancellationToken)
    {
        var openResult = DpkgChangelogReader.FromFile(changelogPath);
        if (!openResult.IsSuccess)
        {
            return null;
        }

        using var reader = openResult.Value;

        while (true)
        {
            var readResult = await reader.ReadChangelogEntryAsync(cancellationToken).ConfigureAwait(false);
            if (!readResult.IsSuccess || !readResult.Value.HasValue)
            {
                return null;
            }

            var entry = readResult.Value.Value;
            if (entry.Version.UpstreamVersion != currentUpstreamVersion)
            {
                return entry;
            }
        }
    }

    /// <summary>
    /// Warns when a version does not increment the patch version by one, which is what a monthly
    /// servicing release normally does. This catches a fix picked from the wrong feature band.
    /// </summary>
    internal static void WarnIfNotConsecutive(BuildTarget buildTarget, string component, string previous, string next)
    {
        if (TryGetPatch(previous, out int previousPatch) &&
            TryGetPatch(next, out int nextPatch) &&
            nextPatch != previousPatch + 1)
        {
            Log.Warning($"{buildTarget}: the {component} version jumps from '{previous}' to '{next}'. " +
                        "A servicing release normally increments the patch version by one. " +
                        "Please verify the version that was selected from cve.json.");
        }
    }

    /// <summary>
    /// Resolves the maintainer identity of the changelog entry trailer from environment variables.
    /// </summary>
    /// <remarks>
    /// Uses DEBFULLNAME and DEBEMAIL, the same variables as dch(1). These must be exported in the
    /// shell environment; the recommended place to set them is ~/.bashrc.
    /// </remarks>
    private static bool TryResolveMaintainer(out MaintainerInfo maintainer)
    {
        maintainer = default;

        string? name = Environment.GetEnvironmentVariable("DEBFULLNAME");
        string? email = Environment.GetEnvironmentVariable("DEBEMAIL");

        if (email is not null)
        {
            var match = NameAndEmailPattern().Match(email);
            if (match.Success)
            {
                email = match.Groups["email"].Value;
                if (string.IsNullOrWhiteSpace(name)) name = match.Groups["name"].Value;
            }
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            Log.Fatal("Could not determine the maintainer identity. Set the environment variables " +
                      "DEBFULLNAME and DEBEMAIL. Add the following to ~/.bashrc and reload your shell:\n" +
                      "  export DEBFULLNAME=\"Your Name\"\n" +
                      "  export DEBEMAIL=\"your.email@example.com\"");
            return false;
        }

        name = name.Trim();
        email = email.Trim();

        // A changelog entry trailer cannot represent these characters.
        if (name.IndexOfAny(UnrepresentableMaintainerCharacters) >= 0 ||
            email.IndexOfAny(UnrepresentableMaintainerCharacters) >= 0)
        {
            Log.Fatal($"The maintainer identity '{name} <{email}>' contains characters that a changelog " +
                      "entry trailer cannot represent.");
            return false;
        }

        maintainer = new MaintainerInfo(name, email);
        return true;
    }

    private static bool TryReadCveFile(FileInfo cveFile, [NotNullWhen(true)] out CveDocument? document)
    {
        document = null;

        try
        {
            using var stream = cveFile.OpenRead();
            document = JsonSerializer.Deserialize<CveDocument>(stream, CveFileJsonOptions);
        }
        catch (FileNotFoundException)
        {
            Log.Fatal($"Could not find the cve file '{cveFile.FullName}'.");
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            Log.Fatal($"Could not find the cve file '{cveFile.FullName}'.");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Log.Fatal($"Insufficient permissions to read the cve file '{cveFile.FullName}'.");
            return false;
        }
        catch (JsonException exception)
        {
            Log.Fatal($"The cve file '{cveFile.FullName}' is not valid JSON.");
            Log.Debug(exception.Message);
            return false;
        }
        catch (Exception exception)
        {
            Log.Fatal($"Reading the cve file '{cveFile.FullName}' unexpectedly failed.");
            Log.Debug(exception.Message);
            return false;
        }

        if (document is null)
        {
            Log.Fatal($"The cve file '{cveFile.FullName}' is empty.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The subset of the Microsoft cve.json schema that is required to write a changelog entry.
    /// </summary>
    /// <remarks>
    /// Collections are nullable, because a monthly disclosure only contains the sections that apply
    /// to it.
    /// </remarks>
    internal sealed record CveDocument(
        Disclosure[]? Disclosures,
        Fix[]? Products,
        Fix[]? Packages,
        Dictionary<string, string[]>? ReleaseCves);

    internal sealed record Disclosure(
        string? Id,
        string[]? Platforms,
        string[]? Description,
        Cna? Cna);

    internal sealed record Cna(string? Impact);

    internal sealed record Fix(
        string? Name,
        string? Release,
        string? Fixed);
}
