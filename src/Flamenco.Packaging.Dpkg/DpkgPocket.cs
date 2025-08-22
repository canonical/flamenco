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

public readonly record struct DpkgPocket : ISpanParsable<DpkgPocket>
{
    public static readonly DpkgPocket Release = new();

    public DpkgPocket() : this(name: "Release", identifier: string.Empty)
    {
    }
    
    private DpkgPocket(string name, string identifier)
    {
        Name = name;
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the name of the pocket.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the identifier of the pocket.
    /// </summary>
    public string Identifier { get; } 
    
    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, Identifier);

    /// <inheritdoc />
    public static DpkgPocket Parse(string? value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgPocket result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgPocket Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgPocket result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }
    
    /// <summary>
    /// Parses a string representation of a debian based repository pocket name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository pocket name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgPocket"/> instance.</returns>
    public static Result<DpkgPocket> Parse(string? value, Location location) => Parse(value.AsSpan(), location);

    /// <summary>
    /// Parses a string representation of a debian based repository pocket name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository pocket name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgPocket"/> instance.</returns>
    public static Result<DpkgPocket> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        var invalidCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        
        if (value.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgPocketName(
                reason: "Pocket name is empty.",
                pocketName: value.ToString(),
                locations: ImmutableList.Create(Location.FromPosition(0).Offset(location)),
                invalidCharacters: invalidCharacters));
        }
        
        Span<char> name = stackalloc char[value.Length];
        bool startOfWord = true;
        var invalidCharacterLocations = ImmutableList<Location>.Empty;
        
        for (var position = 0; position < value.Length; ++position)
        {
            char currentCharacter = value[position];

            if (char.IsAsciiLetterLower(currentCharacter))
            {
                if (startOfWord)
                {
                    name[position] = char.ToUpper(currentCharacter);
                    startOfWord = false;
                }
                else
                {
                    name[position] = currentCharacter;
                }
            }
            else if (currentCharacter == '-' && !startOfWord)
            {
                name[position] = ' ';
                startOfWord = true;
            }
            else
            {
                name[position] = '?';
                invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(position).Offset(location));
                invalidCharacters = invalidCharacters.Add((currentCharacter, position));
            }
        }

        if (startOfWord)
        {
            result = result.WithAnnotation(new MalformedDpkgPocketName(
                reason: "Pocket name ends with a '-' character.",
                pocketName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }
        
        if (invalidCharacterLocations.Count > 0)
        {
            result = result.WithAnnotation(new MalformedDpkgPocketName(
                reason: "Pocket name contains not allowed characters.",
                pocketName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }

        if (result.IsFailure) return result;
        
        return result.WithValue(new DpkgPocket(name.ToString(), value.ToString()));
    }
    
    /// <summary>
    /// Represents an error that occurs when a debian pocket name does not adhere to the specifications.
    /// </summary>
    public class MalformedDpkgPocketName(
        string reason,
        string pocketName,
        ImmutableList<Location> locations,
        ImmutableList<(char InvalidCharacter, int Position)> invalidCharacters
    ) 
        : ErrorBase(
            identifier: "FL0029",
            title: "Malformed dpkg pocket name",
            message: $"Dpkg pocket name '{pocketName}' is malformed. {reason}",
            locations: locations,
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(PocketName), value: pocketName)
                .Add(key: nameof(InvalidCharacters), value: invalidCharacters))
    {
        /// <summary>
        /// The string value of the malformed pocket name
        /// </summary>
        public string PocketName => FromMetadata<string>(nameof(PocketName));
        
        /// <summary>
        /// Contains invalid characters and their position relative to <see cref="PocketName"/>
        /// </summary>
        public ImmutableList<(char InvalidCharacter, int Position)> InvalidCharacters 
            => FromMetadata<ImmutableList<(char InvalidCharacter, int Position)>>(nameof(InvalidCharacters));
    }
}