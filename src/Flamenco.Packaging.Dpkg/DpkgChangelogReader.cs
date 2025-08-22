// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;
using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;

namespace Flamenco.Packaging.Dpkg;

public sealed class DpkgChangelogReader(TextReader textReader, Location location = default) : IDisposable
{
    private int _currentLine = -1;
    
    // in case that a read operation gets canceled, we can pick up where we left off
    private LinePosition _entryStart;
    private LinePosition _entryEnd;
    private ChangelogEntryTitle? _bufferedTitle;

    public Location Location { get; } = location;
    
    public static Result<DpkgChangelogReader> FromFile(string changelogFilePath)
    {
        var result = Result.Success;
        
        try
        {
            var changelogFileStream = new FileStream(changelogFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var textReader = new StreamReader(changelogFileStream);
            
            return new DpkgChangelogReader(textReader, new Location { ResourceLocator = changelogFilePath });
        }
        catch (FileNotFoundException)
        {
            return result.WithAnnotation(new ChangelogFileNotFound(changelogFilePath));
        }
        catch (DirectoryNotFoundException)
        {
            return result.WithAnnotation(new ChangelogFileNotFound(changelogFilePath));
        }
        catch (UnauthorizedAccessException exception)
        {
            return result.WithAnnotation(new InsufficientPermissions(changelogFilePath, exception));
        }
        catch (SecurityException exception)
        {
            return result.WithAnnotation(new InsufficientPermissions(changelogFilePath, exception));
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(new OpenChangelogFileFailed(changelogFilePath, exception));
        }
    }
    
    public static async ValueTask<Result<ChangelogEntry>> ReadFirstChangelogEntryAsync(
        string changelogFilePath,
        CancellationToken cancellationToken = default)
    {
        // open changelog:
        var changelogResult = FromFile(changelogFilePath);
        Result result = changelogResult;
        if (result.IsFailure) return result;
        using var changelog = changelogResult.Value; 
        
        // read first entry:
        var readEntryResult = await changelog.ReadChangelogEntryAsync(cancellationToken).ConfigureAwait(false);
        result = result.Merge(readEntryResult);
        if (result.IsFailure) return result;

        // check that it contains one entry:
        if (readEntryResult.Value.HasValue) return result.WithValue(readEntryResult.Value.Value);
        return result.WithAnnotation(new EmptyChangelog(changelog.Location));
    }
    
    private Location GetCurrentLocation(int? character = default)
    {
        return new Location
        {
            TextSpan = character.HasValue 
                ? new LinePositionSpan(new LinePosition(line: _currentLine, character.Value))
                : new LinePositionSpan(new LinePosition(line: _currentLine))
        }.Offset(Location);
    }
    
    public async ValueTask<Result<ChangelogEntry?>> ReadChangelogEntryAsync(CancellationToken cancellationToken = default)
    {
        var result = new Result();

        // read title:
        var readTitleResult = await ReadTitleAsync(cancellationToken);
        result = result.Merge(readTitleResult);
        if (result.IsFailure) return result;
        if (!readTitleResult.Value.HasValue) return result.WithValue<ChangelogEntry?>(default);
        var title = readTitleResult.Value.Value;
        
        // read trailer:
        var readTrailerResult = await ReadTrailerAsync(cancellationToken);
        result = result.Merge(readTrailerResult);
        if (result.IsFailure) return result;
        var trailer = readTrailerResult.Value;
        
        // combine results:
        return result.WithValue<ChangelogEntry?>(new ChangelogEntry(
            PackageName: title.PackageName,
            Version: title.Version,
            Distributions: title.Distributions,
            Metadata: title.Metadata,
            Maintainer: trailer.Maintainer,
            Date: trailer.Date,
            Location: new Location { TextSpan = new LinePositionSpan(_entryStart, _entryEnd) }.Offset(Location)));
    }
    
    private async ValueTask<Result<ChangelogEntryTitle?>> ReadTitleAsync(CancellationToken cancellationToken)
    {
        var result = new Result();

        while (!_bufferedTitle.HasValue)
        {
            var readLineResult = await ReadLineAsync(cancellationToken);
            result = result.Merge(readLineResult);
            
            if (result.IsFailure) return result;
            var line = readLineResult.Value;
            
            // end of stream
            if (line is null) return result.WithValue<ChangelogEntryTitle?>(default);

            // skip white space between changelog entries
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (char.IsWhiteSpace(line[0]))
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: $"While searching for the start of a changelog entry, found line {_currentLine + 1}, " +
                            "which contains non-whitespace characters, but starts with a whitespace characters." +
                            "Changelog header lines are not allowed to start with a non-whitespace character.",
                    location: GetCurrentLocation(),
                    metadata: ImmutableDictionary<string, object?>.Empty.Add("Line", line)));
            }

            var parseTitleResult = ChangelogEntryTitle.Parse(line, GetCurrentLocation());
            result = result.Merge(parseTitleResult);

            if (parseTitleResult.HasValue)
            {
                _entryStart = new LinePosition(line: _currentLine, character: 0);
                _bufferedTitle = parseTitleResult.Value;
            }
            if (result.IsFailure) return result;
        }
        
        var title = _bufferedTitle;
        _bufferedTitle = null;
        return result.WithValue(title);
    }

    private async ValueTask<Result<string>> ReadTrailerLineAsync(CancellationToken cancellationToken)
    {
        var result = new Result();
        string? line;

        do
        {
            var readLineResult = await ReadLineAsync(cancellationToken);
            result = result.Merge(readLineResult);

            if (result.IsFailure) return result;
            line = readLineResult.Value;

            // end of stream
            if (line is null)
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "Reached end of file, before a changelog entry trailer was found.",
                    location: GetCurrentLocation(),
                    metadata: ImmutableDictionary<string, object?>.Empty.Add("Line", line)));
            }
        } while (string.IsNullOrWhiteSpace(line) || line is [' ', ' ', ..]);

        return result.WithValue(line);
    }
    
    private async ValueTask<Result<ChangelogEntryTrailer>> ReadTrailerAsync(CancellationToken cancellationToken)
    {
        var result = new Result();
        
        var readTrailerLineResult = await ReadTrailerLineAsync(cancellationToken);
        result = result.Merge(readTrailerLineResult);
        if (result.IsFailure) return result;
        
        string line = readTrailerLineResult.Value;
        _entryEnd = new LinePosition(line: _currentLine, character: line.Length - 1);

        var parseTrailerLineResult = ChangelogEntryTrailer.Parse(line, GetCurrentLocation());
        result = result.Merge(parseTrailerLineResult);
        if (result.IsFailure) return result;

        return result.WithValue(parseTrailerLineResult.Value);
    }

    private async ValueTask<Result<string?>> ReadLineAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? line = await textReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            ++_currentLine;
            return line;
        }
        catch (OperationCanceledException)
        {
            return new OperationCanceled();
        }
        catch (Exception exception)
        {
            textReader.Dispose();
            return new ReadingChangelogFailed(GetCurrentLocation(), exception);
        }
    }
    
    public void Dispose()
    {
        textReader.Dispose();
    }
   
    private record struct ChangelogEntryTitle(
        DpkgName PackageName,
        DpkgVersion Version,
        ImmutableArray<DpkgSuite> Distributions,
        ImmutableDictionary<string, string> Metadata)
    {
        static readonly Regex Pattern = new Regex(
            pattern: @"\A(?<PackageName>[a-z0-9]{1}[a-z0-9\+\-\.]+)\s+\((?<Version>[A-Za-z0-9\.\-\+\:\~]*)\)(?:\s+(?<Series>[a-z0-9]{1}[a-z0-9\+\-\.]+))+\s*;(?:(?:\s*(?<MetadataKey>[a-zA-Z0-9]{1}[a-zA-Z0-9\-]*)=(?<MetadataValue>[a-zA-Z0-9\-]*))(?:,\s*(?<MetadataKey>[a-zA-Z0-9]{1}[a-zA-Z0-9\-]*)=(?<MetadataValue>[a-zA-Z0-9\-]*))*)?\s*\z",
            options: RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    
        public static Result<ChangelogEntryTitle> Parse(string line, Location location)
        {
            var result = new Result();
            var match = Pattern.Match(line);
            
            if (!match.Success)
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "Changelog title is malformed.",
                    location: location,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }

            var versionMatch = match.Groups["Version"];
            var parseVersionResult = DpkgVersion.Parse(
                value: versionMatch.ValueSpan, 
                location: new Location
                {
                    TextSpan = new LinePositionSpan(
                        start: versionMatch.Index,
                        end: versionMatch.Index + versionMatch.Length - 1)
                }.Offset(location));
            
            if (parseVersionResult.IsFailure)
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "The version string that is specified in the changelog title is malformed.",
                    location: location,
                    innerAnnotations: parseVersionResult.Annotations,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }

            result = result.Merge(parseVersionResult);
            
            if (match.Groups["MetadataKey"].Captures.Count != match.Groups["MetadataValue"].Captures.Count)
            {
                // This state should never be reachable. The regex should enforce to have a same amount. 
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "This should be an unreachable error, please open a bug report! "+
                            "The amount of metadata keys and metadata values does not match.",
                    location: location,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }

            var metadata =
                match.Groups["MetadataKey"].Captures
                .Select(capture => capture.Value)
                .Zip(match.Groups["MetadataValue"].Captures
                .Select(capture => capture.Value))
                .ToImmutableDictionary(keySelector: x => x.First, elementSelector: x=> x.Second);

            var packageNameMatch = match.Groups["PackageName"];

            var parseNameResult = DpkgName.Parse(
                value: packageNameMatch.ValueSpan,
                location: new Location
                {
                    TextSpan = new LinePositionSpan(
                        start: packageNameMatch.Index,
                        end: packageNameMatch.Index + packageNameMatch.Length - 1)
                }.Offset(location));
            
            if (parseNameResult.IsFailure)
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "The package name string that is specified in the changelog title is malformed.",
                    location: location,
                    innerAnnotations: parseNameResult.Annotations,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }

            result = result.Merge(parseNameResult);

            var distributionCaptures = match.Groups["Series"].Captures;
            var distributions = ImmutableArray.CreateBuilder<DpkgSuite>(initialCapacity: distributionCaptures.Count);
            
            foreach (Capture distributionCapture in distributionCaptures)
            {
                var parseSuiteResult = DpkgSuite.Parse(
                    value: distributionCapture.ValueSpan,
                    location: new Location
                    {
                        TextSpan = new LinePositionSpan(
                            start: distributionCapture.Index,
                            end: distributionCapture.Index + distributionCapture.Length - 1)
                    }.Offset(location));
            
                if (parseSuiteResult.IsFailure)
                {
                    return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                        reason: "The package distribution string that is specified in the changelog title is malformed.",
                        location: location,
                        innerAnnotations: parseSuiteResult.Annotations,
                        metadata: ImmutableDictionary<string, object?>.Empty
                            .Add(key: "Line", value: line)));
                }

                result = result.Merge(parseNameResult);
                distributions.Add(parseSuiteResult.Value);
            }
            
            return result.WithValue(new ChangelogEntryTitle(
                PackageName: parseNameResult.Value,
                Version: parseVersionResult.Value,
                Distributions: distributions.ToImmutable(),
                Metadata: metadata));
        }
    }

    private record struct ChangelogEntryTrailer(
        MaintainerInfo Maintainer,
        DateTimeOffset Date)
    {
        const string DateFormat = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
    
        static readonly Regex Pattern = new Regex(
            pattern: @"\A -- (?<Name>\S+(?: \S+)*) (?:<(?<Email>.+)>)  (?<Date>.+)\z",
            options: RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        
        public static Result<ChangelogEntryTrailer> Parse(string line, Location location)
        {
            var result = new Result();
            var match = Pattern.Match(line);
            
            if (!match.Success)
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "The changelog trailer is malformed.",
                    location: location,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }

            if (!DateTimeOffset.TryParseExact(
                    input: match.Groups["Date"].Value,
                    format: DateFormat,
                    formatProvider: CultureInfo.InvariantCulture.DateTimeFormat,
                    styles: DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var date))
            {
                return result.WithAnnotation(new MalformedDpkgChangelogEntry(
                    reason: "The date-time string that is specified in the changelog trailer is malformed.",
                    location: location,
                    metadata: ImmutableDictionary<string, object?>.Empty
                        .Add(key: "Line", value: line)));
            }
            
            return new ChangelogEntryTrailer(
                Maintainer: new MaintainerInfo(match.Groups["Name"].Value, match.Groups["Email"].Value), 
                Date: date);
        }
    }
    
    public class ChangelogFileNotFound(
        string filePath) 
    : ErrorBase(
        identifier: "FL0013",
        title: "Changelog file not found",
        message: $"Could not find changelog file '{filePath}'",
        locations: ImmutableList.Create(new Location { ResourceLocator = filePath })) {}
    
    public class InsufficientPermissions(
        string filePath, 
        Exception exception) 
    : ErrorBase(
        identifier: "FL0014",
        title: "Insufficient permissions to access changelog file",
        message: $"Insufficient permissions to access changelog file '{filePath}'",
        locations: ImmutableList.Create(new Location { ResourceLocator = filePath }),
        innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception))) {}
    
    public class OpenChangelogFileFailed(
        string filePath, 
        Exception exception) 
    : ErrorBase(
        identifier: "FL0015",
        title: "Opening changelog file failed",
        message: $"Trying to open changelog file '{filePath}' unexpectedly failed",
        locations: ImmutableList.Create(new Location { ResourceLocator = filePath }),
        innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception))) {}
    
    public class EmptyChangelog(
        Location location)
        : ErrorBase(
            identifier: "FL0016",
            title: "Empty changelog file",
            message: $"Changelog file '{location}' is empty",
            locations: ImmutableList.Create(location)) {}
    
    public class MalformedDpkgChangelogEntry(
        string reason,
        Location location,
        ImmutableList<IAnnotation>? innerAnnotations = null,
        ImmutableDictionary<string, object?>? metadata = null) 
    : ErrorBase(
        identifier: "FL0020",
        title: "Malformed dpkg changelog entry",
        message: $"Malformed dpkg changelog entry. {reason}",
        helpLink: new Uri("https://manpages.ubuntu.com/manpages/en/man5/deb-changelog.5.html"),
        locations: ImmutableList.Create(location),
        innerAnnotations: innerAnnotations,
        metadata: metadata) {}
    
    public class ReadingChangelogFailed(
        Location location,
        Exception exception) 
    : ErrorBase(
        identifier: "FL0021",
        title: "Reading changelog file failed",
        message: $"Reading from changelog file unexpectedly failed.",
        locations: ImmutableList.Create(location),
        innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception))) {}
}