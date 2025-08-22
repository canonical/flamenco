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
    /// The default <see cref="Epoch"/> value that can be assumed in case, it is omitted from the string representation.
    /// </summary>
    public const uint MaxEpochValue = int.MaxValue;

    /// <summary>
    /// An <see cref="DpkgVersion"/> instance with an empty string representation.
    /// </summary>
    public static readonly DpkgVersion Empty = new DpkgVersion(
        epoch: null, 
        epochValue: DefaultEpochValue,
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
    /// <param name="epochValue">Value used for the initialization of <see cref="EpochValue"/>.</param>
    /// <param name="upstreamVersion">Value used for the initialization of <see cref="UpstreamVersion"/>.</param>
    /// <param name="revertedUpstreamVersion">Value used for the initialization of <see cref="RevertedUpstreamVersion"/>.</param>
    /// <param name="realUpstreamVersion">Value used for the initialization of <see cref="RealUpstreamVersion"/>.</param>
    /// <param name="revision">Value used for the initialization of <see cref="Revision"/>.</param>
    /// <param name="debianRevision">Value used for the initialization of <see cref="DebianRevision"/>.</param>
    /// <param name="ubuntuRevision">Value used for the initialization of <see cref="UbuntuRevision"/>.</param>
    protected DpkgVersion(
        string? epoch,
        uint epochValue,
        string upstreamVersion,
        string? revertedUpstreamVersion,
        string? realUpstreamVersion,
        string? revision,
        string? debianRevision,
        string? ubuntuRevision)
    {
        Epoch = epoch;
        EpochValue = epochValue;
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
    /// convention: for example, if you uploaded <c>2.3-3</c>, and now you need to go backwards to upstream <c>2.2</c>,
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
    /// <remarks>
    /// Based on <see href="https://git.launchpad.net/ubuntu/+source/dpkg/tree/lib/dpkg/version.c?id=1c6190706e06d4f577e378a7aeaa81f506cb49c6#n140">dpkg(1) implementation</see>.
    /// </remarks>
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

                // A has more digits than B, therefore A is larger
                if (i < a.Length && char.IsAsciiDigit(a[i]))
                    return 1;
                
                // A has more digits than A, therefore B is larger
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
    public static DpkgVersion Parse(string? value, IFormatProvider? formatProvider)
    {
        var parsingResult = Parse(value.AsSpan(), Location.Unspecified);
        parsingResult.ThrowIfError(error => new FormatException(error.Message));
        return parsingResult.Value;
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
        var parsingResult = Parse(value.AsSpan(), Location.Unspecified);
        return parsingResult.TryGetValue(out debVersion) && parsingResult.IsSuccess;
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
        var parsingResult = Parse(value, Location.Unspecified);
        parsingResult.ThrowIfError(error => new FormatException(error.Message));
        return parsingResult.Value;
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
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out debVersion) && parsingResult.IsSuccess;
    }
    
    public static Result<DpkgVersion> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        // ReSharper disable SuggestVarOrType_Elsewhere
        if (value.Length == 0) return Empty;

        var result = ParseEpoch(location, value,
            out ReadOnlySpan<char> epoch,
            out uint epochValue,
            out int upstreamVersionOffset);
        if (result.IsFailure) return result;

        result = result.Merge(ParseUpstreamVersionAndRevision(location, value, upstreamVersionOffset,
            out ReadOnlySpan<char> upstreamVersion,
            out int revisionOffset,
            out ReadOnlySpan<char> revision));
        if (result.IsFailure) return result;

        result = result.Merge(ParseDebianAndUbuntuRevision(location, value, revisionOffset,
            out ReadOnlySpan<char> debianRevision,
            out int ubuntuRevisionOffset,
            out ReadOnlySpan<char> ubuntuRevision));
        if (result.IsFailure) return result;

        result = result.Merge(ParseRevertedAndRealUpstreamVersion(location, value, upstreamVersion, upstreamVersionOffset,
            out bool hasRealUpstreamVersionDelimiter,
            out ReadOnlySpan<char> revertedUpstreamVersion,
            out ReadOnlySpan<char> realUpstreamVersion));
        if (result.IsFailure) return result;
        // ReSharper restore SuggestVarOrType_Elsewhere
        
        return result.WithValue(
            new DpkgVersion(
                epoch: epoch.Length > 0 ? epoch.ToString() : null,
                epochValue: epochValue,
                upstreamVersion: upstreamVersion.ToString(),
                revertedUpstreamVersion: hasRealUpstreamVersionDelimiter ? revertedUpstreamVersion.ToString() : null,
                realUpstreamVersion: hasRealUpstreamVersionDelimiter ? realUpstreamVersion.ToString() : null,
                revision: revisionOffset >= 0 ? revision.ToString() : null,
                debianRevision: revisionOffset >= 0 ? debianRevision.ToString() : null,
                ubuntuRevision: ubuntuRevisionOffset >= 0 ? ubuntuRevision.ToString() : null));
    }
    
    private static Result ParseEpoch(
        Location location,
        ReadOnlySpan<char> version,
        out ReadOnlySpan<char> epoch,
        out uint epochValue,
        out int upstreamVersionOffset)
    {
        epochValue = 0;
        bool epochValueTooLarge = false;
        var invalidEpochCharacters = ImmutableList<(char InvalidEpochCharacter, int Position)>.Empty;
        var invalidEpochCharacterLocations = ImmutableList<Location>.Empty;
        
        for (var position = 0; position < version.Length; ++position)
        {
            char currentCharacter = version[position];
            
            if (currentCharacter == EpochDelimiter)
            {
                epoch = version[..position];
                upstreamVersionOffset = position + 1;

                if (epoch.IsEmpty)
                {
                    return new MalformedDpkgVersionString(
                        reason: "Epoch is empty.",
                        version: version.ToString(),
                        locations: ImmutableList.Create(Location.FromPosition(position).Offset(parent: location)),
                        metadata: ImmutableDictionary<string, object?>.Empty);
                }
                if (!invalidEpochCharacterLocations.IsEmpty)
                {
                    return new MalformedDpkgVersionString(
                        reason: $"Epoch '{epoch}' is not an unsigned integer.",
                        version: version.ToString(),
                        locations: invalidEpochCharacterLocations,
                        metadata: ImmutableDictionary<string, object?>.Empty
                            .Add(key: "InvalidCharacters", value: invalidEpochCharacters)
                            .Add(key: "Epoch", value: epoch.ToString()));
                }
                if (epochValueTooLarge)
                {
                    return new MalformedDpkgVersionString(
                        reason: $"Numerical value of epoch '{epoch}' is too large.",
                        version: version.ToString(),
                        locations: invalidEpochCharacterLocations,
                        metadata: ImmutableDictionary<string, object?>.Empty
                            .Add(key: "InvalidCharacters", value: invalidEpochCharacters)
                            .Add(key: "Epoch", value: epoch.ToString()));
                }
                if (upstreamVersionOffset >= version.Length)
                {
                    return new MalformedDpkgVersionString(
                        reason: $"Upstream version is empty.",
                        version: version.ToString(),
                        locations: ImmutableList.Create(Location.FromPosition(position).Offset(parent: location)),
                        metadata: ImmutableDictionary<string, object?>.Empty
                            .Add(key: "Epoch", value: epoch.ToString()));
                }
                
                return Result.Success;
            }

            if (char.IsAsciiDigit(currentCharacter))
            {
                // I think it is computational cheaper to always calculate this than
                // checking every time if it even makes sense to do:
                epochValue = unchecked(epochValue * 10u + (uint)(currentCharacter - '0'));
                epochValueTooLarge = epochValueTooLarge && epochValue > MaxEpochValue;
            }
            else
            {
                invalidEpochCharacters = invalidEpochCharacters.Add((currentCharacter, position));
                invalidEpochCharacterLocations = invalidEpochCharacterLocations.Add(
                    Location.FromPosition(position).Offset(parent: location));
            }
        }
        
        epoch = ReadOnlySpan<char>.Empty;
        epochValue = DefaultEpochValue;
        upstreamVersionOffset = 0;
        return Result.Success;
    }

    private static Result ParseUpstreamVersionAndRevision(
        Location location,
        ReadOnlySpan<char> version, 
        int upstreamVersionOffset,
        out ReadOnlySpan<char> upstreamVersion,
        out int revisionOffset,
        out ReadOnlySpan<char> revision)
    {
        revisionOffset = -1;
        revision = ReadOnlySpan<char>.Empty;
        var invalidRevisionCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        var invalidRevisionCharacterLocations = ImmutableList<Location>.Empty;
        
        for (int position = version.Length - 1; position >= upstreamVersionOffset; --position)
        {
            char currentCharacter = version[position];
            
            if (currentCharacter == RevisionDelimiter)
            {
                revisionOffset = position + 1;
                upstreamVersion = version[upstreamVersionOffset..position];
                
                if (revisionOffset >= version.Length)
                {
                    return new MalformedDpkgVersionString(
                        reason: "Revision is empty.",
                        version: version.ToString(),
                        locations: ImmutableList.Create(Location.FromPosition(position).Offset(parent: location)),
                        metadata: ImmutableDictionary<string, object?>.Empty);
                }
                
                revision = version[revisionOffset..];

                if (!invalidRevisionCharacterLocations.IsEmpty)
                {
                    return new MalformedDpkgVersionString(
                        reason: $"Revision '{revision}' contains invalid characters.",
                        version: version.ToString(),
                        locations: invalidRevisionCharacterLocations,
                        metadata: ImmutableDictionary<string, object?>.Empty
                            .Add(key: "InvalidCharacters", value: invalidRevisionCharacters)
                            .Add(key: "Revision", value: revision.ToString()));    
                }
                
                return Result.Success;
            }
            else if (!IsAllowedRevisionCharacter(currentCharacter))
            {
                invalidRevisionCharacters = invalidRevisionCharacters.Add((currentCharacter, position));
                invalidRevisionCharacterLocations = invalidRevisionCharacterLocations.Add(
                    Location.FromPosition(position).Offset(parent: location));
            }
        }

        upstreamVersion = version[upstreamVersionOffset..];
        return Result.Success;
    }

    private static Result ParseDebianAndUbuntuRevision(
        Location location,
        ReadOnlySpan<char> version,
        int revisionOffset,
        out ReadOnlySpan<char> debianRevision,
        out int ubuntuRevisionOffset,
        out ReadOnlySpan<char> ubuntuRevision)
    {
        ubuntuRevisionOffset = -1;
        ubuntuRevision = ReadOnlySpan<char>.Empty;
        
        if (revisionOffset < 0)
        {
            debianRevision = ReadOnlySpan<char>.Empty;
            return Result.Success;
        }

        debianRevision = version.Slice(start: revisionOffset);
        var ubuntuRevisionDelimiter = UbuntuRevisionDelimiter.AsSpan();
        var ubuntuRevisionDelimiterLocations = ImmutableList<Location>.Empty;
        
        for (int position = revisionOffset, ubuntuRevisionDelimiterIndex = 0; position < version.Length; ++position)
        {
            char currentCharacter = version[position];
            
            if (currentCharacter == ubuntuRevisionDelimiter[ubuntuRevisionDelimiterIndex])
            {
                if (++ubuntuRevisionDelimiterIndex < ubuntuRevisionDelimiter.Length) continue;

                int delimiterStart = position - ubuntuRevisionDelimiter.Length + 1;
                ubuntuRevisionDelimiterLocations = ubuntuRevisionDelimiterLocations.Add(
                    new Location
                    {
                        TextSpan = new LinePositionSpan(
                            start: new LinePosition(line: 0, character: delimiterStart),
                            end: new LinePosition(line: 0, character: position))
                    }
                    .Offset(parent: location));
                
                if (ubuntuRevisionOffset < 0)
                {
                    debianRevision = version[revisionOffset..delimiterStart];
                    ubuntuRevisionOffset = position + 1;
                    ubuntuRevision = version[ubuntuRevisionOffset..];
                }
            }
            
            ubuntuRevisionDelimiterIndex = 0;
        }

        Result result = Result.Success;

        if (ubuntuRevisionDelimiterLocations.Count > 1)
        {
            result = result.WithAnnotation(new MultipleUbuntuRevisionDelimiterInVersionString(
                version, ubuntuRevisionDelimiterLocations));
        }
        
        return result;
    }

    private static Result ParseRevertedAndRealUpstreamVersion(
        Location location,
        ReadOnlySpan<char> version,
        ReadOnlySpan<char> upstreamVersion,
        int upstreamVersionOffset,
        out bool hasRealUpstreamVersionDelimiter,
        out ReadOnlySpan<char> revertedUpstreamVersion,
        out ReadOnlySpan<char> realUpstreamVersion)
    {
        int upstreamVersionBoundary = upstreamVersionOffset + upstreamVersion.Length;
        
        hasRealUpstreamVersionDelimiter = false;
        revertedUpstreamVersion = ReadOnlySpan<char>.Empty;
        realUpstreamVersion = ReadOnlySpan<char>.Empty;
        var realUpstreamVersionDelimiter = RealUpstreamVersionDelimiter.AsSpan();
        var realUpstreamVersionDelimiterLocations = ImmutableList<Location>.Empty;
        
        var invalidCharacterPositions = ImmutableList<Location>.Empty;
        
        for (int position = upstreamVersionOffset, realUpstreamVersionDelimiterIndex = 0;
             position < upstreamVersionBoundary; ++position)
        {
            char currentCharacter = version[position];
            
            if (version[position] == realUpstreamVersionDelimiter[realUpstreamVersionDelimiterIndex])
            {
                if (++realUpstreamVersionDelimiterIndex < realUpstreamVersionDelimiter.Length) continue;

                int delimiterStart = position - realUpstreamVersionDelimiter.Length + 1;
                realUpstreamVersionDelimiterLocations = realUpstreamVersionDelimiterLocations.Add(
                    new Location
                    {
                        TextSpan = new LinePositionSpan(
                            start: new LinePosition(line: 0, character: delimiterStart),
                            end: new LinePosition(line: 0, character: position))
                    }
                    .Offset(parent: location));
            
                if (!hasRealUpstreamVersionDelimiter)
                {
                    hasRealUpstreamVersionDelimiter = true;
                    revertedUpstreamVersion = version[upstreamVersionOffset..delimiterStart];
                    int realUpstreamVersionOffset = position + 1;
                    realUpstreamVersion = version[realUpstreamVersionOffset..upstreamVersionBoundary];
                }
            }
            else if (!IsAllowedUpstreamVersionCharacter(currentCharacter))
            {
                invalidCharacterPositions = invalidCharacterPositions.Add(
                    Location.FromPosition(position).Offset(parent: location));
            }
        
            realUpstreamVersionDelimiterIndex = 0;
        }
        
        Result result = new Result();

        if (realUpstreamVersionDelimiterLocations.Count > 1)
        {
            result = result.WithAnnotation(new MultipleRealUpstreamVersionDelimiterInVersionString(
                version, realUpstreamVersionDelimiterLocations));
        }

        if (!invalidCharacterPositions.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgVersionString(
                reason: $"Upstream version '{upstreamVersion}' contains invalid characters.",
                version: version.ToString(),
                locations: invalidCharacterPositions,
                metadata: ImmutableDictionary<string, object?>.Empty
                    .Add(key: "UpstreamVersion", value: upstreamVersion.ToString())));
        }
        
        return result;
    }

    private static bool IsAllowedUpstreamVersionCharacter(char character) 
        => char.IsAsciiLetterOrDigit(character) 
           || character == '.' || character == '+' || character == '-' || character == ':' || character == '~';

    private static bool IsAllowedRevisionCharacter(char character) 
        => char.IsAsciiLetterOrDigit(character) || character == '+' || character == '.' || character == '~';

    public static explicit operator string(DpkgVersion dpkgVersion) => dpkgVersion.ToString();
    public static explicit operator DpkgVersion(string debVersion) => Parse(debVersion, formatProvider: null);
    
    public class MalformedDpkgVersionString(
        string reason,
        string version,
        ImmutableList<Location> locations,
        ImmutableDictionary<string, object?> metadata) 
    : AnnotationBase(
        identifier: "FL0017",
        title: "Bad dpkg version syntax",
        message: $"Version '{version}' is malformed. {reason}",
        severity: AnnotationSeverity.Error,
        warningLevel: WarningLevels.Error,
        locations: locations,
        helpLink: new("https://manpages.ubuntu.com/manpages/en/man7/deb-version.7.html"),
        metadata: metadata.Add(key: nameof(Version), value: version))
    {
        public string Version => (string)Metadata[nameof(Version)]!;
    }
    
    public class MultipleUbuntuRevisionDelimiterInVersionString(
        ReadOnlySpan<char> version, 
        ImmutableList<Location> locations) 
    : AnnotationBase(
        identifier: "FL0018",
        title: $"Multiple '{UbuntuRevisionDelimiter}' delimiter in version string.",
        message:  $"Version '{version}' contains multiple '{UbuntuRevisionDelimiter}' delimiter. " +
                  "Note that only the first is used to parse the ubuntu revision.",
        description: $"The '{UbuntuRevisionDelimiter}' delimiter within the debian revision component of a " +
                     "version string is a convention to indicate changes which are only applied to Ubuntu " +
                     "packages." +
                     $"This version string contains more than one '{UbuntuRevisionDelimiter}' delimiter, which " +
                     "is unusual and therefore could indicate an issue with the format of the version string.",
        helpLink: new("https://github.com/canonical/ubuntu-maintainers-handbook/blob/main/VersionStrings.md"),
        severity: AnnotationSeverity.Warning,
        warningLevel: WarningLevels.MinorWarning,
        locations: locations,
        metadata: EmptyMetadata.Add(key: nameof(Version), value: version.ToString()))
    {
        public string Version => (string)Metadata[nameof(Version)]!;
    }
    
    public class MultipleRealUpstreamVersionDelimiterInVersionString(
        ReadOnlySpan<char> version, 
        ImmutableList<Location> locations) 
    : AnnotationBase(
        identifier: "FL0019",
        title: $"Multiple '{RealUpstreamVersionDelimiter}' delimiter in version string.",
        message:  $"Version '{version}' contains multiple '{RealUpstreamVersionDelimiter}' delimiter. " +
                  "Note that only the first is used to parse the real upstream version.",
        description: $"The '{RealUpstreamVersionDelimiter}' delimiter within the upstream component of a version " +
                     "string is a convention to indicate that the package contains a different version. This is for " +
                     "example used to roll back an upload. In this situation the version number can not simply " +
                     "lowered, because the package manager ignores all packages with a lower version number than " +
                     "the already installed package version." +
                     $"This version string contains more than one '{RealUpstreamVersionDelimiter}' delimiter, which " +
                     "is unusual and therefore could indicate an issue with the format of the version string.",
        helpLink: new("https://www.debian.org/doc/debian-policy/ch-controlfields.html#epochs-should-be-used-sparingly"),
        severity: AnnotationSeverity.Warning,
        warningLevel: WarningLevels.MinorWarning,
        locations: locations,
        metadata: EmptyMetadata.Add(key: nameof(Version), value: version.ToString()))
    {
        public string Version => (string)Metadata[nameof(Version)]!;
    }
}
