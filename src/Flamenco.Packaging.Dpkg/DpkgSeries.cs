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
/// Represents an immutable instance of the name of a distribution series used in Debian packaging
/// to refer to specific release targets for a package.
/// </summary>
public readonly record struct DpkgSeries : ISpanParsable<DpkgSeries>
{
    private DpkgSeries(string identifier)
    {
        Identifier = identifier;
    }
    
    /// <summary>
    /// Gets the identifier of the series.
    /// </summary>
    public string Identifier { get; }

    /// <inheritdoc />
    public static DpkgSeries Parse(string? value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgSeries result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgSeries Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgSeries result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }
    
    /// <summary>
    /// Parses a string representation of a debian series name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian series name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgSeries"/> instance.</returns>
    public static Result<DpkgSeries> Parse(string? value, Location location) => Parse(value.AsSpan(), location);
    
    /// <summary>
    /// Parses a string representation of a debian series name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian series name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgSeries"/> instance.</returns>
    public static Result<DpkgSeries> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        var invalidCharacters = ImmutableList<(char invalidRevisionCharacter, int position)>.Empty;
        
        if (value.IsEmpty)
        {
            return result.WithAnnotation(new MalformedDpkgSeriesName(
                reason: "Series name is empty.",
                seriesName: value.ToString(),
                locations: ImmutableList.Create(Location.FromPosition(0).Offset(location)),
                invalidCharacters: invalidCharacters));
        }

        var invalidCharacterLocations = ImmutableList<Location>.Empty;

        for (var position = 0; position < value.Length; ++position)
        {
            char currentCharacter = value[position];

            if (!char.IsAsciiLetterLower(currentCharacter))
            {
                invalidCharacterLocations = invalidCharacterLocations.Add(Location.FromPosition(position).Offset(location));
                invalidCharacters = invalidCharacters.Add((currentCharacter, position));
            }
        }
        
        if (invalidCharacterLocations.Count > 0)
        {
            return result.WithAnnotation(new MalformedDpkgSeriesName(
                reason: "Series name contains not allowed characters.",
                seriesName: value.ToString(),
                locations: invalidCharacterLocations,
                invalidCharacters: invalidCharacters));
        }
        
        return result.WithValue(new DpkgSeries(value.ToString()));
    }

    /// <summary>
    /// Represents an error that occurs when a debian series name does not adhere to the specifications.
    /// </summary>
    public class MalformedDpkgSeriesName(
        string reason,
        string seriesName,
        ImmutableList<Location> locations,
        ImmutableList<(char InvalidCharacter, int Position)> invalidCharacters
    ) 
        : ErrorBase(
            identifier: "FL0028",
            title: "Malformed dpkg series name",
            message: $"Dpkg series name '{seriesName}' is malformed. {reason}",
            locations: locations,
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(SeriesName), value: seriesName)
                .Add(key: nameof(InvalidCharacters), value: invalidCharacters))
    {
        /// <summary>
        /// The string value of the malformed series name
        /// </summary>
        public string SeriesName => FromMetadata<string>(nameof(SeriesName));
        
        /// <summary>
        /// Contains invalid characters and their position relative to <see cref="SeriesName"/>
        /// </summary>
        public ImmutableList<(char InvalidCharacter, int Position)> InvalidCharacters 
            => FromMetadata<ImmutableList<(char InvalidCharacter, int Position)>>(nameof(InvalidCharacters));
    }
}