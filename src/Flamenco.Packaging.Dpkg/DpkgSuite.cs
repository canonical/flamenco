// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Flamenco.Packaging.Dpkg;

/// <summary>
/// A combination of a series and a pocket.
/// </summary>
/// <example><c>noble-updates</c></example>
public readonly record struct DpkgSuite : ISpanParsable<DpkgSuite>
{
    /// <summary>
    /// The series this suite refers to.
    /// </summary>
    public required DpkgSeries Series { get; init; }
    
    /// <summary>
    /// The pocket this suite refers to.
    /// </summary>
    public required DpkgPocket Pocket { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.IsNullOrEmpty(Pocket.Identifier) 
            ? Series.Identifier 
            : $"{Series.Identifier}-{Pocket.Identifier}";
    }

    /// <inheritdoc />
    public static DpkgSuite Parse(string value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(string? value, IFormatProvider? formatProvider, out DpkgSuite result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }

    /// <inheritdoc />
    public static DpkgSuite Parse(ReadOnlySpan<char> value, IFormatProvider? formatProvider = null)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.ThrowIfErrorOrReturnValue(error => new FormatException(error.Message));
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? formatProvider, out DpkgSuite result)
    {
        var parsingResult = Parse(value, Location.Unspecified);
        return parsingResult.TryGetValue(out result) && parsingResult.IsSuccess;
    }
    
    /// <summary>
    /// Parses a string representation of a debian based repository suite name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository suite name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgSuite"/> instance.</returns>
    public static Result<DpkgSuite> Parse(string? value, Location location) => Parse(value.AsSpan(), location);

    /// <summary>
    /// Parses a string representation of a debian based repository suite name and performs validation.
    /// </summary>
    /// <param name="value">The string representation of the debian based repository suite name.</param>
    /// <param name="location">The location where <paramref name="value"/> was found (default: unspecified).</param>
    /// <returns>The parsing result that may contain a <see cref="DpkgSuite"/> instance.</returns>
    public static Result<DpkgSuite> Parse(ReadOnlySpan<char> value, Location location = default)
    {
        var result = Result.Success;
        
        int separationIndex = value.IndexOf('-');

        if (separationIndex < 0)
        {
            var paringSeriesResult = DpkgSeries.Parse(value, location);
            result = result.Merge(paringSeriesResult);
            if (result.IsFailure) return result;

            return result.WithValue(new DpkgSuite { Series = paringSeriesResult.Value, Pocket = DpkgPocket.Release });
        }
        else
        {
            var seriesSpan = value.Slice(start: 0, length: separationIndex);
            var seriesLocation = new Location { TextSpan = new LinePositionSpan(
                start: 0, 
                end: seriesSpan.Length - 1) }
                .Offset(location);

            int pocketStart = separationIndex + 1; 
            var pocketSpan = value.Slice(start: pocketStart);
            var pocketLocation = new Location { TextSpan = new LinePositionSpan(
                start: pocketStart,
                end: value.Length - 1) }
                .Offset(location);
            
            var paringSeriesResult = DpkgSeries.Parse(seriesSpan, seriesLocation);
            var parsingPocketResult = DpkgPocket.Parse(pocketSpan, pocketLocation);
            
            result = result.Merge(paringSeriesResult).Merge(parsingPocketResult);
            if (result.IsFailure) return result;

            return result.WithValue(new DpkgSuite
            {
                Series = paringSeriesResult.Value,
                Pocket = parsingPocketResult.Value,
            });
        }
    } 
}