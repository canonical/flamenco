using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Flamenco.Packaging.Dpkg;

/// <summary>
/// A representation of the Debian/Ubuntu package version format, as defined in the man page
/// <see href="https://manpages.ubuntu.com/manpages/noble/en/man7/deb-version.7.html">deb-version(7)</see>.
/// </summary>
public class DpkgVersion : 
    IComparable,
    IComparable<DpkgVersion>,
    IEquatable<DpkgVersion>,
    ISpanParsable<DpkgVersion>
{
    /// <summary>
    /// The character that indicates the boundary between the epoch and remaining deb version string representation.  
    /// </summary>
    public const char EpochDelimiter = ':';
    
    /// <summary>
    /// The character that indicates the boundary between the upstream version and debian revision
    /// in a deb version string representation.
    /// </summary>
    public const char RevisionDelimiter = '-';
    
    /// <summary>
    /// The sequence of characters that indicates the boundary between the debian and ubuntu revision (if present)
    /// in a deb version string representation.
    /// </summary>
    public const string UbuntuRevisionDelimiter = "ubuntu";
    
    /// <summary>
    /// The sequence of characters that indicates the boundary between the reverted and real upstream version
    /// (if present) in a deb version string representation.
    /// </summary>
    public const string RealUpstreamVersionDelimiter = "+really";
    
    /// <summary>
    /// The default <see cref="Epoch"/> value that can be assumed in case, it is omitted from the string representation.
    /// </summary>
    public const uint DefaultEpochValue = 0;

    /// <summary>
    /// An <see cref="DpkgVersion"/> instance with an empty string representation.
    /// </summary>
    public static readonly DpkgVersion Empty = new DpkgVersion(
        epoch: null, 
        upstreamVersion: string.Empty, 
        revertedUpstreamVersion: null, 
        realUpstreamVersion: null,
        revision: null,
        debianRevision: null, 
        ubuntuRevision: null);
    
    /// <summary>
    /// Initializes a new <see cref="DpkgVersion"/> instance using the values provided for initialization. 
    /// </summary>
    /// <remarks>
    /// The values provided for initialization are not validated.
    /// Make sure the values conform with the expected formats.
    /// </remarks>
    /// <param name="epoch">Value used for the initialization of <see cref="Epoch"/>.</param>
    /// <param name="upstreamVersion">Value used for the initialization of <see cref="UpstreamVersion"/>.</param>
    /// <param name="revertedUpstreamVersion">Value used for the initialization of <see cref="RevertedUpstreamVersion"/>.</param>
    /// <param name="realUpstreamVersion">Value used for the initialization of <see cref="RealUpstreamVersion"/>.</param>
    /// <param name="revision">Value used for the initialization of <see cref="Revision"/>.</param>
    /// <param name="debianRevision">Value used for the initialization of <see cref="DebianRevision"/>.</param>
    /// <param name="ubuntuRevision">Value used for the initialization of <see cref="UbuntuRevision"/>.</param>
    protected DpkgVersion(
        string? epoch,
        string upstreamVersion,
        string? revertedUpstreamVersion,
        string? realUpstreamVersion,
        string? revision,
        string? debianRevision,
        string? ubuntuRevision)
    {
        Epoch = epoch;
        EpochValue = epoch is null ? DefaultEpochValue : uint.Parse(epoch);
        UpstreamVersion = upstreamVersion;
        RevertedUpstreamVersion = revertedUpstreamVersion;
        RealUpstreamVersion = realUpstreamVersion;
        Revision = revision;
        DebianRevision = debianRevision;
        UbuntuRevision = ubuntuRevision;
    }
    
    /// <summary>
    /// A single (generally small) unsigned integer. It may be omitted, in which case <see cref="Epoch"/> will be
    /// <see langword="null"/>. If it is omitted then <see cref="UpstreamVersion"/> may not contain any <c>:</c> (colon).
    /// </summary>
    /// <remarks>
    /// <b>Epochs should be used sparingly!</b>
    /// <br/><br/>
    /// The purpose of epochs is to cope with situations where the upstream version numbering scheme changes
    /// and to allow us to leave behind serious mistakes. If you think that increasing the epoch is the right solution,
    /// you should consult experienced developers for your distribution and get consensus before doing so.
    /// <br/><br/>
    /// Epochs should not be used when a package needs to be rolled back. In that case, use the <c>+really</c>
    /// convention: for example, if you uploaded <c>2.3-3</c> and now you need to go backwards to upstream <c>2.2</c>,
    /// call your reverting upload something like <c>2.3+really2.2-1</c>. Eventually, when we upload upstream
    /// <c>2.4</c>, the <c>+really</c> part can go away.
    /// </remarks>
    public string? Epoch { get; }
    
    /// <summary>
    /// Gets numeric value represented by <see cref="Epoch"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="Epoch"/> is <see langword="null"/>, the value of <see cref="DefaultEpochValue"/> is used.
    /// </remarks>
    public uint EpochValue { get; }
    
    /// <summary>
    /// The main part of the version number.
    /// </summary>
    /// <remarks>
    /// It is usually the version number of the original (“upstream”) package from which the
    /// <c>.deb</c> file has been made, if this is applicable.  Usually this will be in the same
    /// format as that specified by the upstream author(s); however, it may need to be reformatted
    /// to fit into the package management system's format and comparison scheme.
    ///
    /// The upstream-version may contain only alphanumerics (“A-Za-z0-9”) and the characters
    /// <c>.</c> <c>+</c> <c>-</c> <c>:</c> <c>~</c> (full stop, plus, hyphen, colon, tilde)
    /// and should start with a digit. If <see cref="Revision"/> is <see langword="null"/> then
    /// hyphens are not allowed; if <see cref="Epoch"/> is <see langword="null"/> then colons are not allowed.
    /// </remarks>
    public string UpstreamVersion { get; }
    
    /// <summary>
    /// If <see cref="UpstreamVersion"/> contains a <c>+really</c> delimiter, it will contain the reverted upstream
    /// version (the part of the string before the <c>+really</c> delimiter); otherwise it will be
    /// <see langword="null"/>.
    /// </summary>
    public string? RevertedUpstreamVersion { get; }
    
    /// <summary>
    /// If <see cref="UpstreamVersion"/> contains a <c>+really</c> delimiter, it will contain the real upstream
    /// version (the part of the string after the <c>+really</c> delimiter); otherwise it will be
    /// <see langword="null"/>.
    /// </summary>
    public string? RealUpstreamVersion { get; }
    
    /// <summary>
    /// Gets <see cref="RealUpstreamVersion"/> if it is not <see langword="null"/>;
    /// otherwise it gets <see cref="UpstreamVersion"/>.
    /// </summary>
    public string EffectiveUpstreamVersion => RealUpstreamVersion ?? UpstreamVersion;
    
    /// <summary>
    /// This part of the version number specifies the version of the deb package based on the
    /// <see cref="UpstreamVersion"/>.
    /// Native packages do not have a revision, in which case <see cref="Revision"/> will be <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// See the man page dpkg-source(1) and the Ubuntu Packaging Guide (packaging.ubuntu.com) for more details
    /// about native packages.
    /// </remarks>
    public string? Revision { get; }
    
    /// <summary>
    /// If <see cref="Revision"/> is <see langword="null"/> it will be <see langword="null"/>.
    /// If <see cref="Revision"/> contains an <c>ubuntu</c> delimiter, it will contain
    /// the debian revision (the part of the string before the <c>ubuntu</c> delimiter).
    /// If <see cref="Revision"/> does not contain an <c>ubuntu</c> delimiter, it will be equal to
    /// <see cref="Revision"/>. 
    /// </summary>
    public string? DebianRevision { get; }
    
    /// <summary>
    /// If <see cref="Revision"/> is not <see langword="null"/> and contains an<c>ubuntu</c> delimiter, it will contain
    /// the ubuntu revision (the part of the string after the <c>ubuntu</c> delimiter); otherwise it will be
    /// <see langword="null"/>.
    /// </summary>
    public string? UbuntuRevision { get; }

    /// <summary>
    /// Returns a string that represents the current object in the format described in the
    /// <see href="https://manpages.ubuntu.com/manpages/noble/en/man7/deb-version.7.html">deb-version(7)</see> man page.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var version = new StringBuilder();
        
        if (Epoch is not null)
            version.Append(Epoch).Append(EpochDelimiter);
        
        version.Append(UpstreamVersion);

        if (Revision is not null)
            version.Append(RevisionDelimiter).Append(Revision);
        
        return version.ToString();
    }

    /// <inheritdoc cref="Object.GetHashCode()"/>
    public override int GetHashCode() => HashCode.Combine(Epoch, UpstreamVersion, DebianRevision, UbuntuRevision);

    /// <inheritdoc cref="Object.Equals(object?)"/>
    public override bool Equals(object? other) => other switch 
    {
        DpkgVersion debVersion => Equals(debVersion),
        _ => false
    };

    /// <inheritdoc cref="IEquatable{T}.Equals(T?)"/>
    public bool Equals(DpkgVersion? other) => CompareTo(other) == 0;

    /// <summary>
    /// Indicates whether the two <see cref="DpkgVersion"/> instances are equal to each other.
    /// </summary>
    /// <param name="a">The first <see cref="DpkgVersion"/> instance to compare with <paramref name="b"/>.</param>
    /// <param name="b">The second <see cref="DpkgVersion"/> instance to compare with <paramref name="a"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="a"/> is equal to <paramref name="b"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool operator ==(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return b is null;
        return a.Equals(b);
    }
        
    /// <summary>
    /// Indicates whether the two <see cref="DpkgVersion"/> instances are not equal to each other.
    /// </summary>
    /// <param name="a">The first <see cref="DpkgVersion"/> instance to compare with <paramref name="b"/>.</param>
    /// <param name="b">The second <see cref="DpkgVersion"/> instance to compare with <paramref name="a"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="a"/> is not equal to <paramref name="b"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool operator !=(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return b is not null;
        return !a.Equals(b);
    }
    
    /// <inheritdoc cref="IComparable.CompareTo(object?)" />
    public int CompareTo(object? other) => other switch 
    {
        null => 1,
        DpkgVersion debVersion => CompareTo(debVersion),
        _ => throw new ArgumentException(
            paramName: nameof(other),
            message: $"Can't compare type {other.GetType().FullName} with type {typeof(DpkgVersion).FullName}.")
    };
    
    /// <inheritdoc cref="IComparable{T}.CompareTo(T?)" />
    public int CompareTo(DpkgVersion? other)
    {
        if (other is null) return 1;
        
        if (EpochValue > other.EpochValue) return 1;
        if (EpochValue < other.EpochValue) return -1;

        int weight = ComparePart(UpstreamVersion, other.UpstreamVersion);
        if (weight != 0) return weight;
        
        return ComparePart(Revision, other.Revision);
        
        static int ComparePart(string? a, string? b)
        {
            a ??= string.Empty;
            b ??= string.Empty;
            
            for (int i = 0, j = 0; i < a.Length || j < b.Length;)
            {
                while ((i < a.Length && !char.IsAsciiDigit(a[i])) || 
                       (j < b.Length && !char.IsAsciiDigit(b[i])))
                {
                    int weightA = GetCharacterWeight(a, i);
                    int weightB = GetCharacterWeight(b, j);
                    
                    if (weightA != weightB)
                        return weightA - weightB;

                    i++;
                    j++;
                }
                
                // skip leading zeros;
                while (i < a.Length && a[i] == '0') i++;
                while (j < b.Length && b[j] == '0') j++;
                
                // stores the first numerical difference when comparing from left to right
                int mostSignificantNumericalDifference = 0;
                
                while (i < a.Length && char.IsAsciiDigit(a[i]) &&
                       j < b.Length && char.IsAsciiDigit(b[j])) 
                {
                    if (mostSignificantNumericalDifference == 0)
                        mostSignificantNumericalDifference = a[i] - b[j];
                    
                    ++i;
                    ++j;
                }

                // a has more digits than b, therefore a is larger
                if (i < a.Length && char.IsAsciiDigit(a[i]))
                    return 1;
                
                // b has more digits than a, therefore b is larger
                if (j < b.Length && char.IsAsciiDigit(b[j]))
                    return -1;
                
                if (mostSignificantNumericalDifference != 0)
                    return mostSignificantNumericalDifference;
            }

            return 0;
        }

        static int GetCharacterWeight(string value, int index)
        {
            if (index >= value.Length) return 0;
            
            char character = value[index];
            
            if (char.IsAsciiDigit(character))
                return 0;
            if (char.IsAsciiLetter(character))
                return character;
            if (character == '~')
                return -1;
            
            return character + 256;
        }
    }
    
    public static bool operator >(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return false;
        return a.CompareTo(b) > 0;
    }
    
    public static bool operator >=(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return b is null;
        return a.CompareTo(b) >= 0;
    }
    
    public static bool operator <(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return b is not null;
        return a.CompareTo(b) < 0;
    }
    
    public static bool operator <=(DpkgVersion? a, DpkgVersion? b)
    {
        if (a is null) return true;
        return a.CompareTo(b) <= 0;
    }
    
    /// <summary>
    /// Parses a <see langword="string"/> into a <see cref="DpkgVersion"/> value.
    /// </summary>
    /// <param name="value">The <see langword="string"/> to parse.</param>
    /// <param name="formatProvider">
    /// An <see langword="object"/> that provides culture-specific formatting
    /// information about <paramref name="value"/>.
    /// </param>
    /// <returns>The result of parsing <paramref name="value"/>.</returns>
    /// <remarks>
    /// The <paramref name="formatProvider"/> is ignored during parsing, because <see langword="string"/>
    /// representations of <see cref="DpkgVersion"/> must be parsed culture invariant.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// The format of <paramref name="value"/> violates the format described in the
    /// <see href="https://manpages.ubuntu.com/manpages/noble/en/man7/deb-version.7.html">deb-version(7)</see> man page.
    /// </exception>
    public static DpkgVersion Parse(string value, IFormatProvider? formatProvider = null)
    {
        return ParseCore(
            value: value ?? throw new ArgumentNullException(paramName: nameof(value)), 
            throwOnError: true)!;
    }
    
    /// <summary>
    /// Tries to parse a <see langword="string"/> into a <see cref="DpkgVersion"/> value.
    /// </summary>
    /// <param name="value">The <see langword="string"/> to parse.</param>
    /// <param name="formatProvider">
    /// An <see langword="object"/> that provides culture-specific formatting
    /// information about <paramref name="value"/>.
    /// </param>
    /// <param name="debVersion">
    /// When this method returns, contains the result of successfully parsing <paramref name="value"/>
    /// or <see langword="null"/> on failure.
    /// </param>
    /// <returns><see langword="true"/> if s was successfully parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        string? value, 
        IFormatProvider? formatProvider, 
        [NotNullWhen(returnValue: true)] out DpkgVersion? debVersion)
    {
        debVersion = ParseCore(value, throwOnError: false);
        return debVersion is not null;
    }
    
    /// <summary>
    /// Parses a span of characters into a <see cref="DpkgVersion"/> value.
    /// </summary>
    /// <param name="value">The span of characters to parse.</param>
    /// <param name="formatProvider">
    /// An <see langword="object"/> that provides culture-specific formatting
    /// information about <paramref name="value"/>.
    /// </param>
    /// <returns>The result of parsing <paramref name="value"/>.</returns>
    /// <remarks>
    /// The <paramref name="formatProvider"/> is ignored during parsing, because <see langword="string"/>
    /// representations of <see cref="DpkgVersion"/> must be parsed culture invariant.
    /// </remarks>
    /// <exception cref="FormatException">
    /// The format of <paramref name="value"/> violates the format described in the
    /// <see href="https://manpages.ubuntu.com/manpages/noble/en/man7/deb-version.7.html">deb-version(7)</see> man page.
    /// </exception>
    public static DpkgVersion Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        return ParseCore(value, throwOnError: true)!;
    }
    
    /// <summary>
    /// Tries to parse a span of characters into a <see cref="DpkgVersion"/> value.
    /// </summary>
    /// <param name="value">The span of characters to parse.</param>
    /// <param name="formatProvider">
    /// An <see langword="object"/> that provides culture-specific formatting
    /// information about <paramref name="value"/>.
    /// </param>
    /// <param name="debVersion">
    /// When this method returns, contains the result of successfully parsing <paramref name="value"/>
    /// or <see langword="null"/> on failure.
    /// </param>
    /// <returns><see langword="true"/> if s was successfully parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> value, 
        IFormatProvider? formatProvider, 
        [NotNullWhen(returnValue: true)] out DpkgVersion? debVersion)
    {
        debVersion = ParseCore(value, throwOnError: false);
        return debVersion is not null;
    }
    
    protected static DpkgVersion? ParseCore(ReadOnlySpan<char> value, bool throwOnError)
    {
        if (value.Length == 0) return DpkgVersion.Empty;
        
        return TryParseCore(
            value, throwOnError,
            out var epoch,
            out var upstreamVersion,
            out var hasRealUpstreamVersionDelimiter,
            out var revertedUpstreamVersion,
            out var realUpstreamVersion,
            out var hasRevision,
            out var revision,
            out var debianRevision,
            out var hasUbuntuRevision,
            out var ubuntuRevision)
            ? new DpkgVersion(
                epoch: epoch.Length > 0 ? epoch.ToString() : null,
                upstreamVersion: upstreamVersion.ToString(),
                revertedUpstreamVersion: hasRealUpstreamVersionDelimiter ? revertedUpstreamVersion.ToString() : null,
                realUpstreamVersion: hasRealUpstreamVersionDelimiter ? realUpstreamVersion.ToString() : null,
                revision: hasRevision ? revision.ToString() : null,
                debianRevision: hasRevision ? debianRevision.ToString() : null,
                ubuntuRevision: hasUbuntuRevision ? ubuntuRevision.ToString() : null)
            : null;
    }

    protected static bool TryParseCore(
        ReadOnlySpan<char> value, 
        bool throwOnError, 
        out ReadOnlySpan<char> epoch,
        out ReadOnlySpan<char> upstreamVersion,
        out bool hasRealUpstreamVersionDelimiter,
        out ReadOnlySpan<char> revertedUpstreamVersion,
        out ReadOnlySpan<char> realUpstreamVersion,
        out bool hasRevision,
        out ReadOnlySpan<char> revision,
        out ReadOnlySpan<char> debianRevision,
        out bool hasUbuntuRevision,
        out ReadOnlySpan<char> ubuntuRevision)
    {
        // NOTE: Yes, I have thought of using regular expressions, but I always found edge cases where the pattern
        // did not allow for a valid version input with the .NET Regex engine. Writing the parser on our own has the
        // added benefit of being able to produce more advanced error messages.
        // 
        // If anyone still thinks to give regular expressions a go, here is my last attempt:
        //
        // const string MatchUpstreamVersion = @"(?:(?<UpstreamVersion>[A-Za-z0-9\\.\\+\\~]+))";
        // const string MatchEpochAndUpstreamVersion = @"(?:(?:(?<Epoch>\\d+):)(?<UpstreamVersion>[A-Za-z0-9\\.\\+\\:\\~]+))";
        // const string MatchUpstreamVersionAndRevision = @"(?:(?<UpstreamVersion>[A-Za-z0-9\.\+\-\~]+)(?:-(?<Revision>[A-Za-z0-9\+\.\~]+)))";
        // const string MatchAll = @"(?:(?:(?<Epoch>\d+):)(?<UpstreamVersion>[A-Za-z0-9\.\+\-\:\~]+)(?:-(?<Revision>[A-Za-z0-9\+\.\~]+)))";
        // 
        // Regex Pattern = new Regex(
        //     pattern: @$"\A(?:{MatchUpstreamVersion}|{MatchEpochAndUpstreamVersion}|{MatchUpstreamVersionAndRevision}|{MatchAll})\z", 
        //     options: RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        //
        // Example of false negatives: "1:1"
        // Note: Interestingly this worked with different regex engines, see for example https://regex101.com/r/1htECL/1 

        epoch = ReadOnlySpan<char>.Empty;
        upstreamVersion = value;
        
        for (int i = 0, invalidCharacterPosition = -1; i < value.Length; ++i)
        {
            if (value[i] == EpochDelimiter)
            {
                epoch = value.Slice(start: 0, length: i);
                
                if (invalidCharacterPosition >= 0)
                {
                    if (throwOnError) throw new FormatException(
                        message: $"Version '{value}' has bad syntax. The epoch '{epoch}' contains non digit characters '{value[invalidCharacterPosition]}'. See the deb-version(7) man page.");

                    hasRealUpstreamVersionDelimiter = false;
                    revertedUpstreamVersion = ReadOnlySpan<char>.Empty;
                    realUpstreamVersion = ReadOnlySpan<char>.Empty;
                    hasRevision = false;
                    revision = ReadOnlySpan<char>.Empty;
                    debianRevision = ReadOnlySpan<char>.Empty;
                    hasUbuntuRevision = false;
                    ubuntuRevision = ReadOnlySpan<char>.Empty;
                    return false;
                }
                
                upstreamVersion = value.Slice(start: i+1);
                break;
                
            }
            
            if (invalidCharacterPosition < 0 && !IsAllowedEpochCharacter(value[i]))
            {
                invalidCharacterPosition = i;
            }
        }

        hasRevision = false;
        revision = ReadOnlySpan<char>.Empty;
        
        for (int i = upstreamVersion.Length - 1, invalidCharacterPosition = -1; i >= 0; --i)
        {
            if (upstreamVersion[i] == RevisionDelimiter)
            {
                hasRevision = true;
                revision = upstreamVersion.Slice(start: i+1);
                
                if (invalidCharacterPosition >= 0)
                {
                    if (throwOnError) throw new FormatException(
                        message: $"Version '{value}' has bad syntax. The revision '{revision}' contains an invalid character '{upstreamVersion[invalidCharacterPosition]}'. See the deb-version(7) man page.");
                    
                    hasRealUpstreamVersionDelimiter = false;
                    revertedUpstreamVersion = ReadOnlySpan<char>.Empty;
                    realUpstreamVersion = ReadOnlySpan<char>.Empty;
                    debianRevision = ReadOnlySpan<char>.Empty;
                    hasUbuntuRevision = false;
                    ubuntuRevision = ReadOnlySpan<char>.Empty;
                    return false;
                }
                
                upstreamVersion = upstreamVersion.Slice(start: 0, length: i); 
                break;
            }
            
            if (invalidCharacterPosition < 0 && !IsAllowedRevisionCharacter(upstreamVersion[i]))
            {
                invalidCharacterPosition = i;
            }
        }
        
        hasUbuntuRevision = false;
        ubuntuRevision = ReadOnlySpan<char>.Empty;

        if (hasRevision)
        {
            debianRevision = revision;
            
            var ubuntuRevisionDelimiter = UbuntuRevisionDelimiter.AsSpan();
            for (int i = 0, j = 0; i < revision.Length; ++i)
            {
                if (revision[i] == ubuntuRevisionDelimiter[j])
                {
                    ++j;
                    if (j == ubuntuRevisionDelimiter.Length)
                    {
                        hasUbuntuRevision = true;
                        debianRevision = revision.Slice(start: 0, length: i - ubuntuRevisionDelimiter.Length + 1);
                        ubuntuRevision = revision.Slice(start: i + 1);
                        break;
                    }
                }
                else
                {
                    j = 0;
                }
            }
        }
        else
        {
            debianRevision = ReadOnlySpan<char>.Empty;
        }
        
        hasRealUpstreamVersionDelimiter = false;
        revertedUpstreamVersion = ReadOnlySpan<char>.Empty;
        realUpstreamVersion = ReadOnlySpan<char>.Empty;
        var realUpstreamVersionDelimiter = RealUpstreamVersionDelimiter.AsSpan();
        
        for (int i = 0, j = 0; i < upstreamVersion.Length; ++i)
        {
            if (!IsAllowedUpstreamVersionCharacter(upstreamVersion[i]))
            {
                return throwOnError
                    ? throw new FormatException(message: $"Version '{value}' has bad syntax. The upstream-version '{upstreamVersion}' contains an invalid character '{upstreamVersion[i]}'. See the deb-version(7) man page.")
                    : false;
            }

            if (!hasRealUpstreamVersionDelimiter && upstreamVersion[i] == realUpstreamVersionDelimiter[j])
            {
                ++j;
                if (j == realUpstreamVersionDelimiter.Length)
                {
                    hasRealUpstreamVersionDelimiter = true;
                    realUpstreamVersion = upstreamVersion.Slice(start: i + 1);
                    revertedUpstreamVersion = upstreamVersion.Slice(start: 0, length: i - realUpstreamVersionDelimiter.Length + 1);
                }
            }
        }

        return true;

        static bool IsAllowedEpochCharacter(char character) => char.IsAsciiDigit(character);

        static bool IsAllowedUpstreamVersionCharacter(char character) => char.IsAsciiLetterOrDigit(character) 
            || character == '.' || character == '+' || character == '-' || character == ':' || character == '~';
    
        static bool IsAllowedRevisionCharacter(char character) => char.IsAsciiLetterOrDigit(character) 
            || character == '+' || character == '.' || character == '~';
    }
    
    public static explicit operator string(DpkgVersion dpkgVersion) => dpkgVersion.ToString();
    public static explicit operator DpkgVersion(string debVersion) => Parse(debVersion);
}