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
// See original source: https://github.com/dotnet/roslyn/blob/9abd314036e89274d3deff101bacca8551bfba5c/src/Compilers/Core/Portable/Text/LinePosition.cs

namespace Flamenco;

/// <summary>
/// Immutable representation of a line number and position within a source text.
/// </summary>
public readonly struct LinePosition : IEquatable<LinePosition>, IComparable<LinePosition>
{
    private readonly int _character;

    /// <summary>
    /// A <see cref="LinePosition"/> that represents position 0 at line 0.
    /// </summary>
    public static LinePosition Zero => default;

    /// <summary>
    /// Initializes a new instance of a <see cref="LinePosition"/> with the given line.
    /// </summary>
    /// <param name="line">
    /// The line of the line position. The first line in a file is defined as line 0 (zero based line numbering).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="line"/> is less than zero.
    /// </exception>
    public LinePosition(int line)
    {
        if (line < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(line), 
                actualValue: line,
                message: "Line number has to be zero or larger");
        }
        
        Line = line;
        _character = -1;
    }
    
    /// <summary>
    /// Initializes a new instance of a <see cref="LinePosition"/> with the given line and character.
    /// </summary>
    /// <param name="line">
    /// The line of the line position. The first line in a file is defined as line 0 (zero based line numbering).
    /// </param>
    /// <param name="character">
    /// The character position in the line. The first character in a line is defined as character 0
    /// (zero based character numbering).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="line"/> or <paramref name="character"/>is less than zero.
    /// </exception>
    public LinePosition(int line, int character)
    {
        if (line < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(line), 
                actualValue: line,
                message: "Line number has to be zero or larger");
        }

        if (character < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(character), 
                actualValue: character,
                message: "Character number has to be zero or larger");
        }
        
        Line = line;
        _character = character;
    }
    
    /// <summary>
    /// The line number. The first line in a file is defined as line 0 (zero based line numbering).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The character position within the line. The first character in a line is defined as character 0
    /// (zero based character numbering).
    /// </summary>
    public int Character => _character >= 0 ? _character : 0;

    /// <summary>
    /// Provides a string representation for <see cref="LinePosition"/>.
    /// </summary>
    /// <remarks>
    /// The string representation will use one based numbering for line and position. 
    /// </remarks>
    /// <example>line 9 character 5</example>
    public override string ToString()
    {
        if (Line >= 0) return $"line {Line + 1} character {Character + 1}"; 
            
        return $"line {Line + 1}";
    }
    
    /// <summary>
    /// Provides a hash function for <see cref="LinePosition"/>.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Line, Character);

    /// <summary>
    /// Determines whether two <see cref="LinePosition"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public override bool Equals(object? other) => other is LinePosition linePosition && Equals(linePosition);

    /// <summary>
    /// Determines whether two <see cref="LinePosition"/> are the same.
    /// </summary>
    /// <param name="other">The object to compare.</param>
    public bool Equals(LinePosition other) => other.Line == this.Line && other._character == this._character;

    /// <inheritdoc />
    public int CompareTo(LinePosition other)
    {
        int result = Line.CompareTo(other.Line);
        return (result != 0) ? result : _character.CompareTo(other._character);
    }
    
    /// <summary>
    /// Determines whether two <see cref="LinePosition"/> are the same.
    /// </summary>
    public static bool operator ==(LinePosition left, LinePosition right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="LinePosition"/> are different.
    /// </summary>
    public static bool operator !=(LinePosition left, LinePosition right) => !left.Equals(right);
    
    /// <summary>
    /// Determines whether one <see cref="LinePosition"/> is larger than another.
    /// </summary>
    public static bool operator >(LinePosition left, LinePosition right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="LinePosition"/> is larger than or equal to another.
    /// </summary>
    public static bool operator >=(LinePosition left, LinePosition right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Determines whether one <see cref="LinePosition"/> is less than another.
    /// </summary>
    public static bool operator <(LinePosition left, LinePosition right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="LinePosition"/> is less than or equal to another.
    /// </summary>
    public static bool operator <=(LinePosition left, LinePosition right) => left.CompareTo(right) <= 0;
}