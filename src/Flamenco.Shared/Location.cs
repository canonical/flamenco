// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// NOTE: This is remotely inspired by Roslyn (Copyright .NET Foundation and Contributors).
// See original source: https://github.com/dotnet/roslyn/blob/e88d57317793c85fa8567389d3bc83649efded95/src/Compilers/Core/Portable/Diagnostic/Location.cs

namespace Flamenco;

/// <summary>
/// Immutable representation of a source text location.
/// </summary>
public readonly struct Location : IEquatable<Location>
{
    public static Location FromPosition(int position) => new ()
    {
        ResourceLocator = null,
        TextSpan = new LinePositionSpan(position: new LinePosition(line: 0, character: position))
    };
    
    /// <summary>
    /// Represents an unspecified location.
    /// </summary>
    public static readonly Location Unspecified = default;
    
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    /// <remarks>
    /// Most often will represent a file path.
    /// </remarks>
    public string? ResourceLocator { get; init; }

    /// <summary>
    /// Gets the boundaries of the specific source text within the <see cref="ResourceLocator"/>.  
    /// </summary>
    public LinePositionSpan? TextSpan { get; init; }

    /// <summary>
    /// Returns a representation of this location as if it would be part of another location, effectively offsetting it.
    /// </summary>
    /// <param name="parent">
    /// The location where this location is relative to.
    /// </param>
    /// <returns>
    /// A representation of this location as if it would be subset of <paramref name="parent"/>.
    /// </returns>
    public Location Offset(Location parent)
    {
        // TODO: perform bounds check to look of this  
        
        if (parent == Unspecified) return this;

        LinePositionSpan? textSpan;
    
        if (parent.TextSpan.HasValue)
        {
            if (TextSpan.HasValue)
            {
                var offsetTextSpan = parent.TextSpan.Value;
                var relativeTextSpan = TextSpan.Value;

                textSpan = new LinePositionSpan(
                    start: new LinePosition(
                        line: relativeTextSpan.Start.Line + offsetTextSpan.Start.Line,
                        character: relativeTextSpan.Start.Character + offsetTextSpan.Start.Character),
                    end: new LinePosition(
                        line: relativeTextSpan.End.Line + offsetTextSpan.Start.Line,
                        character: relativeTextSpan.End.Character + offsetTextSpan.Start.Character));
            }
            else
            {
                textSpan = parent.TextSpan;
            }
        }
        else
        {
            textSpan = TextSpan;
        }
    
        return new Location
        {
            ResourceLocator = parent.ResourceLocator ?? ResourceLocator,
            TextSpan = textSpan,
        };
    }
    
    /// <summary>
    /// Provides a string representation for <see cref="Location"/>.
    /// </summary>
    /// <remarks>
    /// The string representation will use one based numbering for line and position. 
    /// </remarks>
    /// <example>
    /// <list type="bullet">
    /// <item><c>"at line 9"</c></item>
    /// <item><c>"'file:///tmp/tmp.If9Zph7YUw'"</c></item>
    /// <item><c>"'file:///tmp/tmp.If9Zph7YUw' at line 9 character 5"</c></item>
    /// <item><c>"'file:///tmp/tmp.If9Zph7YUw' from line 9 character 5 to line 12 character 2"</c></item>
    /// </list>
    /// </example>
    public override string ToString()
    {
        return (ResourceLocator is not null, TextSpan.HasValue) switch
        {
            (true, true) => $"'{ResourceLocator}' {TextSpan!.Value}",
            (true, false) => $"'{ResourceLocator}'",
            (false, true) => TextSpan!.Value.ToString(),
            (false, false) => "Unspecified"
        };
    }
    
    /// <summary>
    /// Provides a hash function for <see cref="Location"/>.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(ResourceLocator, TextSpan);

    /// <summary>
    /// Determines whether two <see cref="Location"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public override bool Equals(object? other) => other is Location location && Equals(location);
    
    /// <summary>
    /// Determines whether two <see cref="Location"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public bool Equals(Location other)
    {
        return this.ResourceLocator is null == other.ResourceLocator is null
               && this.TextSpan.HasValue == other.TextSpan.HasValue
               && (ResourceLocator is null || this.ResourceLocator.Equals(other.ResourceLocator))
               && (!TextSpan.HasValue || this.TextSpan.Value.Equals(other.TextSpan!.Value));
    }
    
    /// <summary>
    /// Determines whether two <see cref="Location"/> are the same.
    /// </summary>
    public static bool operator ==(Location left, Location right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="Location"/> are different.
    /// </summary>
    public static bool operator !=(Location left, Location right) => !left.Equals(right);
}