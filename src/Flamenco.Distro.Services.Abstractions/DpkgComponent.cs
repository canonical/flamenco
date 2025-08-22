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

namespace Flamenco.Distro.Services.Abstractions;

/// <summary>
/// The component of a dpkg archive where packages are stored. 
/// </summary>
public readonly record struct DpkgComponent : ISpanParsable<DpkgComponent>
{
    public static readonly DpkgComponent Main = new DpkgComponent();

    public DpkgComponent() : this(identifier: "main")
    {
    }

    private DpkgComponent(string identifier)
    {
        Identifier = identifier;
    }
    
    /// <summary>
    /// Gets the identifier of the component.
    /// </summary>
    public string Identifier { get; }
    
    /// <inheritdoc />
    public override string ToString() => Identifier;

    /// <inheritdoc />
    public static DpkgComponent Parse(string? value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgComponent result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgComponent Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgComponent result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }
    
    /// <summary>
    /// Parses a string representation of a debian based repository component name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository component name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgComponent"/> instance.</returns>
    public static Result<DpkgComponent> Parse(string? value, Location location) => Parse(value.AsSpan(), location);

    /// <summary>
    /// Parses a string representation of a debian based repository component name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository component name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgComponent"/> instance.</returns>
    public static Result<DpkgComponent> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        var invalidCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        
        if (value.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgComponentName(
                reason: "Component name is empty.",
                componentName: value.ToString(),
                locations: ImmutableList.Create(Location.FromPosition(0).Offset(location)),
                invalidCharacters: invalidCharacters));
        }
        
        bool startOfWord = true;
        var invalidCharacterLocations = ImmutableList<Location>.Empty;
        
        for (var position = 0; position < value.Length; ++position)
        {
            char currentCharacter = value[position];

            if (char.IsAsciiLetterLower(currentCharacter))
            {
                startOfWord = false;
            }
            else if (currentCharacter == '-' && !startOfWord)
            {
                startOfWord = true;
            }
            else
            {
                invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(position).Offset(location));
                invalidCharacters = invalidCharacters.Add((currentCharacter, position));
            }
        }

        if (startOfWord)
        {
            result = result.WithAnnotation(new MalformedDpkgComponentName(
                reason: "Component name ends with a '-' character.",
                componentName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }
        
        if (invalidCharacterLocations.Count > 0)
        {
            result = result.WithAnnotation(new MalformedDpkgComponentName(
                reason: "Component name contains not allowed characters.",
                componentName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }

        if (result.IsFailure) return result;
        
        return result.WithValue(new DpkgComponent(value.ToString()));
    }
    
    /// <summary>
    /// Represents an error that occurs when a debian component name does not adhere to the specifications.
    /// </summary>
    public class MalformedDpkgComponentName(
        string reason,
        string componentName,
        ImmutableList<Location> locations,
        ImmutableList<(char InvalidCharacter, int Position)> invalidCharacters
    ) 
        : ErrorBase(
            identifier: "FL0030",
            title: "Malformed dpkg component name",
            message: $"Dpkg component name '{componentName}' is malformed. {reason}",
            locations: locations,
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(ComponentName), value: componentName)
                .Add(key: nameof(InvalidCharacters), value: invalidCharacters))
    {
        /// <summary>
        /// The string value of the malformed component name
        /// </summary>
        public string ComponentName => FromMetadata<string>(nameof(ComponentName));
        
        /// <summary>
        /// Contains invalid characters and their position relative to <see cref="ComponentName"/>
        /// </summary>
        public ImmutableList<(char InvalidCharacter, int Position)> InvalidCharacters 
            => FromMetadata<ImmutableList<(char InvalidCharacter, int Position)>>(nameof(InvalidCharacters));
    }
}
