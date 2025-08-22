// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// NOTE: This is heavily inspired by Roslyn (Copyright .NET Foundation and Contributors).
// See original source: https://github.com/dotnet/roslyn/blob/9abd314036e89274d3deff101bacca8551bfba5c/src/Compilers/Core/Portable/Text/LinePositionSpan.cs

namespace Flamenco;

/// <summary>
/// Immutable span represented by a pair of line number and index within the line.
/// </summary>
public readonly struct LinePositionSpan : IEquatable<LinePositionSpan>
{
    /// <summary>
    /// Creates <see cref="LinePositionSpan"/>.
    /// </summary>
    /// <param name="position">Start and end position.</param>
    public LinePositionSpan(LinePosition position)
    {
        Start = position;
        End = position;
    }
    
    /// <summary>
    /// Creates <see cref="LinePositionSpan"/>.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <exception cref="ArgumentException"><paramref name="end"/> precedes <paramref name="start"/>.</exception>
    public LinePositionSpan(LinePosition start, LinePosition end)
    {
        if (end < start)
        {
            throw new ArgumentException(
                paramName: nameof(end),
                message: $"End position ({end}) precedes start position ({start})");
        }

        Start = start;
        End = end;
    }
    
    /// <summary>
    /// Creates <see cref="LinePositionSpan"/> from a range within one line.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <exception cref="ArgumentException"><paramref name="end"/> precedes <paramref name="start"/>.</exception>
    public LinePositionSpan(int start, int end)
    {
        if (end < start)
        {
            throw new ArgumentException(
                paramName: nameof(end),
                message: $"End position ({end}) precedes start position ({start})");
        }

        Start = new LinePosition(line: 0, character: start);
        End = new LinePosition(line: 0, character: end);
    }
    
    /// <summary>
    /// Gets the start position of the span.
    /// </summary>
    public LinePosition Start { get; }
    
    /// <summary>
    /// Gets the end position of the span.
    /// </summary>
    public LinePosition End { get; }

    /// <summary>
    /// Provides a string representation for <see cref="LinePositionSpan"/>.
    /// </summary>
    /// <remarks>
    /// The string representation will use one based numbering for line and position. 
    /// </remarks>
    /// <example>
    /// <list type="bullet">
    /// <item><c>"at line 9"</c></item>
    /// <item><c>"at line 9 character 5"</c></item>
    /// <item><c>"from line 9 character 5 to line 12 character 2"</c></item>
    /// </list>
    /// </example>
    public override string ToString()
    {
        return Start != End
            ? $"from {Start} to {End}"
            : $"at {Start}";
    }
    
    /// <summary>
    /// Provides a hash function for <see cref="LinePositionSpan"/>.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>
    /// Determines whether two <see cref="LinePositionSpan"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public override bool Equals(object? other) => other is LinePositionSpan linePositionSpan && Equals(linePositionSpan);
    
    /// <summary>
    /// Determines whether two <see cref="LinePositionSpan"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public bool Equals(LinePositionSpan other) => other.Start == this.Start && other.End == this.End;
    
    /// <summary>
    /// Determines whether two <see cref="LinePositionSpan"/> are the same.
    /// </summary>
    public static bool operator ==(LinePositionSpan left, LinePositionSpan right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="LinePositionSpan"/> are different.
    /// </summary>
    public static bool operator !=(LinePositionSpan left, LinePositionSpan right)
    {
        return !left.Equals(right);
    }
}