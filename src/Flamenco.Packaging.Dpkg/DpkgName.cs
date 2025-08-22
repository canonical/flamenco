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

/// <summary>
/// Represents an immutable instance of the name of a debian package.
/// </summary>
public readonly record struct DpkgName : ISpanParsable<DpkgName>
{
    private DpkgName(string identifier)
    {
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the identifier of the debian package.
    /// </summary>
    public string Identifier { get; }
    
    /// <inheritdoc />
    public override string ToString() => Identifier;

    /// <inheritdoc />
    public override int GetHashCode() => Identifier.GetHashCode();
    
    /// <inheritdoc />
    public static DpkgName Parse(string? value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgName result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgName Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgName result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <summary>
    /// Parses a string representation of a debian package name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian package name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgName"/> instance.</returns>
    public static Result<DpkgName> Parse(string? value, Location location) => Parse(value.AsSpan(), location);
    
    /// <summary>
    /// Parses a string representation of a debian package name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian package name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgName"/> instance.</returns>
    public static Result<DpkgName> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        var invalidCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        
        if (value.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgName(
                reason: "Package name is empty.",
                packageName: value.ToString(),
                locations: ImmutableList.Create(Location.FromPosition(0).Offset(location)),
                invalidCharacters: invalidCharacters));
        }

        var invalidCharacterLocations = ImmutableList<Location>.Empty;
        
        if (!char.IsAsciiLetterLower(value[0]) && !char.IsAsciiDigit(value[0]))
        {
            invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(0).Offset(location));
            invalidCharacters = invalidCharacters.Add((value[0], 0));
        }

        for (var position = 1; position < value.Length; ++position)
        {
            char currentCharacter = value[position];

            if (!char.IsAsciiLetterLower(currentCharacter)
                && !char.IsAsciiDigit(currentCharacter)
                && currentCharacter != '-'
                && currentCharacter != '.'
                && currentCharacter != '+')
            {
                invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(position).Offset(location));
                invalidCharacters = invalidCharacters.Add((currentCharacter, position));
            }
        }
        
        if (invalidCharacterLocations.Count > 0)
        {
            return result.WithAnnotation(new MalformedDpkgName(
                reason: "Package name contains not allowed characters.",
                packageName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }
        
        return result.WithValue(new DpkgName(value.ToString()));
    }

    /// <summary>
    /// Represents an error that occurs when a debian package name does not adhere to the specifications.
    /// </summary>
    public class MalformedDpkgName(
        string reason,
        string packageName,
        ImmutableList<Location> locations,
        ImmutableList<(char InvalidCharacter, int Position)> invalidCharacters
        ) 
        : ErrorBase(
            identifier: "FL0026",
            title: "Malformed dpkg package name",
            message: $"Dpkg package name '{packageName}' is malformed. {reason}",
            locations: locations,
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(PackageName), value: packageName)
                .Add(key: nameof(InvalidCharacters), value: invalidCharacters))
    {
        /// <summary>
        /// The string value of the malformed package name
        /// </summary>
        public string PackageName => FromMetadata<string>(nameof(PackageName));
        
        /// <summary>
        /// Contains invalid characters and their position relative to <see cref="PackageName"/>
        /// </summary>
        public ImmutableList<(char InvalidCharacter, int Position)> InvalidCharacters 
            => FromMetadata<ImmutableList<(char InvalidCharacter, int Position)>>(nameof(InvalidCharacters));
    }
}