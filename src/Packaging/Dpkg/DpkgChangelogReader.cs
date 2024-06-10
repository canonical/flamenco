using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Flamenco.Packaging.Dpkg;

public sealed class DpkgChangelogReader(TextReader textReader) : IDisposable
{
    private int _totalLinesRead = 0;
    
    public static DpkgChangelogReader FromFile(string changelogFilePath)
    {
        var changelogFileStream = new FileStream(changelogFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var textReader = new StreamReader(changelogFileStream);
        return new DpkgChangelogReader(textReader);
    }
    
    public static DpkgChangelogReader FromStream(Stream changelogContentStream)
    {
        var textReader = new StreamReader(changelogContentStream);
        return new DpkgChangelogReader(textReader);
    }
    
    public static DpkgChangelogReader FromString(string changelog)
    {
        var textReader = new StringReader(changelog);
        return new DpkgChangelogReader(textReader);
    }
    
    public async Task<ChangelogEntry?> ReadChangelogEntryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? line;
            
            ChangelogEntryTitle? title = default;
            
            do
            {
                ++_totalLinesRead;
                line = await textReader.ReadLineAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                
                // end of stream
                if (line is null) return null; 
                
                // skip white space
                if (line.Length == 0) continue;
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                if (char.IsWhiteSpace(line[0]))
                {
                    throw new FormatException(message: $"Line {_totalLinesRead} contains non white space characters, " + 
                                                       "but starts with a space characters before a changelog entry " + 
                                                       "header could be found. See man page deb-changelog(5).");
                }

                title = ChangelogEntryTitle.Parse(line, _totalLinesRead);
            } while (!title.HasValue);
            
            ChangelogEntryTrailer? trailer = default;

            do
            {
                ++_totalLinesRead;
                line = await textReader.ReadLineAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                if (line is null) // end of stream
                {
                    throw new FormatException(message: "Changelog ends with partial entry that is missing a trailer " +
                                                       "line. See man page deb-changelog(5).");
                }
                
                // TODO: parse changes
                if (line is [' ', ' ', ..]) continue;  
                if (string.IsNullOrWhiteSpace(line)) continue;
                    
                trailer = ChangelogEntryTrailer.Parse(line, _totalLinesRead);
            } while (!trailer.HasValue);

            return new ChangelogEntry(
                PackageName: title.Value.PackageName,
                Version: title.Value.Version,
                Distributions: title.Value.Distributions,
                Metadata: title.Value.Metadata,
                Maintainer: trailer.Value.Maintainer,
                Date: trailer.Value.Date);
        }
        catch
        {
            // We are not able to recover from this state, therefore we can free all allocated resources.
            // This also causes subsequent calls to fail, which is good. We do not want to parse from the
            // middle of the text. 
            textReader.Dispose();
            throw;
        }
    }
    
    public void Dispose()
    {
        textReader.Dispose();
    }
    
    private record struct ChangelogEntryTitle(
        string PackageName,
        DpkgVersion Version,
        ImmutableArray<string> Distributions,
        ImmutableDictionary<string, string> Metadata)
    {
        static readonly Regex Pattern = new Regex(
            pattern: @"\A(?<PackageName>[a-z0-9]{1}[a-z0-9\+\-\.]+)\s+\((?<Version>[A-Za-z0-9\.\-\+\:\~]*)\)(?:\s+(?<Series>[a-z0-9]{1}[a-z0-9\+\-\.]+))+\s*;(?:(?:\s*(?<MetadataKey>[a-zA-Z0-9]{1}[a-zA-Z0-9\-]*)=(?<MetadataValue>[a-zA-Z0-9\-]*))(?:,\s*(?<MetadataKey>[a-zA-Z0-9]{1}[a-zA-Z0-9\-]*)=(?<MetadataValue>[a-zA-Z0-9\-]*))*)?\s*\z",
            options: RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    
        public static ChangelogEntryTitle Parse(string line, int lineNumber)
        {
            var match = Pattern.Match(line);

            if (!match.Success)
            {
                throw new FormatException(message: $"Expected changelog entry title on line {lineNumber}, but the " +
                                          "format does not match the format detailed in man page deb-changelog(5).");
            }

            DpkgVersion version;

            try
            {
                version = DpkgVersion.Parse(match.Groups["Version"].Value);
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    message: $"The version specified in the changelog entry title on line {lineNumber} failed " + 
                             "to parse. See the inner exception for more details.",
                    innerException: exception);
            }

            if (match.Groups["MetadataKey"].Captures.Count != match.Groups["MetadataValue"].Captures.Count)
            {
                // This state should never be reached. The regex should enforce to have a same amount. 
                throw new FormatException(message: "Unexpected parsing error. The amount of metadata keys and " +
                                                   "metadata values does not match.");
            }

            var metadata =
                match.Groups["MetadataKey"].Captures
                .Select(capture => capture.Value)
                .Zip(match.Groups["MetadataValue"].Captures
                .Select(capture => capture.Value))
                .ToImmutableDictionary(keySelector: x => x.First, elementSelector: x=> x.Second);
                
            return new ChangelogEntryTitle(
                PackageName: match.Groups["PackageName"].Value,
                Version: version,
                Distributions: [..match.Groups["Series"].Captures.Select(capture => capture.Value)],
                Metadata: metadata);
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
        
        public static ChangelogEntryTrailer Parse(string line, int lineNumber)
        {
            var match = Pattern.Match(line);
            
            if (!match.Success)
            {
                throw new FormatException(message: $"Expected changelog entry trailer on line {lineNumber}, but the " +
                                                   "format does not match the format detailed in man page " +
                                                   "deb-changelog(5).");
            }

            if (!DateTimeOffset.TryParseExact(
                    input: match.Groups["Date"].Value,
                    format: DateFormat,
                    formatProvider: CultureInfo.InvariantCulture.DateTimeFormat,
                    styles: DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                    out var date))
            {
                throw new FormatException(message: "The format of the date-time string in the changelog entry " +
                                                   $"trailer on line {lineNumber} does not match the format " +
                                                   "specified in man page deb-changelog(5).");
            }
            
            return new ChangelogEntryTrailer(
                Maintainer: new MaintainerInfo(match.Groups["Name"].Value, match.Groups["Email"].Value), 
                Date: date);
        }
    }
}

public record struct MaintainerInfo(string Name, string EmailAddress);

public record ChangelogEntry(
    string PackageName,
    DpkgVersion Version,
    ImmutableArray<string> Distributions,
    ImmutableDictionary<string, string> Metadata,
    MaintainerInfo Maintainer,
    DateTimeOffset Date)
{
    public string? Urgency => CollectionExtensions.GetValueOrDefault(Metadata, key: "urgency");
    
    public string? BinaryOnly => CollectionExtensions.GetValueOrDefault(Metadata, key: "binary-only");
}