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

namespace Flamenco.Packaging.Dpkg;

public readonly record struct DpkgArchitecture : ISpanParsable<DpkgArchitecture>
{
    public static readonly DpkgArchitecture Source = new(identifier: "source"); 
    public static readonly DpkgArchitecture All = new(identifier: "all");
    public static readonly DpkgArchitecture Amd64 = new(identifier: "amd64");
    public static readonly DpkgArchitecture Arm64 = new(identifier: "arm64");
    public static readonly DpkgArchitecture Armel = new(identifier: "armel");
    public static readonly DpkgArchitecture Armhf = new(identifier: "armhf");
    public static readonly DpkgArchitecture I386 = new(identifier: "i386");
    public static readonly DpkgArchitecture Mips64el = new(identifier: "mips64el");
    public static readonly DpkgArchitecture Ppc64el = new(identifier: "ppc64el");
    public static readonly DpkgArchitecture PowerPc = new(identifier: "powerpc");
    public static readonly DpkgArchitecture S390X = new(identifier: "s390x");
    public static readonly DpkgArchitecture RiscV64 = new(identifier: "riscv64");
    
    private DpkgArchitecture(string identifier)
    {
        Identifier = identifier;
    }
    
    /// <summary>
    /// Gets the identifier of the architecture as used by dpkg.
    /// </summary>
    public string Identifier { get; }
    
    /// <inheritdoc />
    public override string ToString() => Identifier;

    /// <inheritdoc />
    public override int GetHashCode() => Identifier.GetHashCode();
    
    /// <inheritdoc />
    public static DpkgArchitecture Parse(string? value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        parsingResult.ThrowIfError(error => new FormatException(error.Message));
        return parsingResult.Value;
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgArchitecture result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgArchitecture Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        parsingResult.ThrowIfError(error => new FormatException(error.Message));
        return parsingResult.Value;
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgArchitecture result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <summary>
    /// Parses a string representation of an architecture identifier as used by dpkg and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the architecture identifier.</param>
    /// <param name="location">The location where value was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgArchitecture"/> instance.</returns>
    public static Result<DpkgArchitecture> Parse(string? value, Location location) => Parse(value.AsSpan(), location);
    
    /// <summary>
    /// Parses a string representation of an architecture identifier as used by dpkg and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the architecture identifier.</param>
    /// <param name="location">The location where value was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgArchitecture"/> instance.</returns>
    public static Result<DpkgArchitecture> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        var invalidCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        
        if (value.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgArchitecture(
                reason: "Architecture identifier is empty.",
                architectureIdentifier: value.ToString(),
                locations: ImmutableList.Create(Location.FromPosition(0).Offset(location)),
                invalidCharacters: invalidCharacters));
        }

        var invalidCharacterLocations = ImmutableList<Location>.Empty;
        
        for (var position = 0; position < value.Length; ++position)
        {
            char currentCharacter = value[position];

            if (!char.IsAsciiLetterLower(currentCharacter) && !char.IsAsciiDigit(currentCharacter))
            {
                invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(position).Offset(location));
                invalidCharacters = invalidCharacters.Add((currentCharacter, position));
            }
        }
        
        if (invalidCharacterLocations.Count > 0)
        {
            return result.WithAnnotation(new MalformedDpkgArchitecture(
                reason: "Architecture identifier contains not allowed characters.",
                architectureIdentifier: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }
        
        return result.WithValue(new DpkgArchitecture(value.ToString()));
    }

    /// <summary>
    /// Represents an error that occurs when an architecture identifier does not adhere to the specifications.
    /// </summary>
    public class MalformedDpkgArchitecture(
        string reason,
        string architectureIdentifier,
        ImmutableList<Location> locations,
        ImmutableList<(char InvalidCharacter, int Position)> invalidCharacters
        ) 
        : ErrorBase(
            identifier: "FL0027",
            title: "Malformed dpkg architecture identifier ",
            message: $"Dpkg architecture identifier '{architectureIdentifier}' is malformed. {reason}",
            locations: locations,
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(ArchitectureIdentifier), value: architectureIdentifier)
                .Add(key: nameof(InvalidCharacters), value: invalidCharacters))
    {
        /// <summary>
        /// The string value of the malformed package name
        /// </summary>
        public string ArchitectureIdentifier => FromMetadata<string>(nameof(ArchitectureIdentifier));
        
        /// <summary>
        /// Contains invalid characters and their position relative to <see cref="ArchitectureIdentifier"/>
        /// </summary>
        public ImmutableList<(char InvalidCharacter, int Position)> InvalidCharacters 
            => FromMetadata<ImmutableList<(char InvalidCharacter, int Position)>>(nameof(InvalidCharacters));
    }
}