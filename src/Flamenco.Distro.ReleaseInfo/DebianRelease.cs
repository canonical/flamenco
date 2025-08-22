// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.ReleaseInfo;

/// <summary>
/// An immutable record of details about an Debian release. 
/// </summary>
public record DebianRelease
{
    private readonly string _stringRepresentation;

    /// <summary>
    /// Gets a value indicating whether the Debian release is declared stable.
    /// </summary>
    /// <remarks>
    /// A stable Debian release is a release that is considered to be production-ready and suitable for widespread use.
    /// </remarks>
    public bool IsStable { get; }

    /// <summary>
    /// The version identifier of Debian release.
    /// </summary>
    /// <example>
    /// <c>12</c>
    /// </example>
    /// <remarks>
    /// Non-stable releases do not have a version identifier. 
    /// </remarks>
    public string? Version { get; }

    /// <summary>
    /// The codename of the Debian release.
    /// </summary>
    /// <example>
    /// <c>"Bookworm"</c>
    /// </example>
    public string Codename { get; }
    
    /// <summary>
    /// Represents the series identifier of an Debian release.
    /// </summary>
    /// <example>
    /// <c>bookworm</c>
    /// </example>
    public DpkgSeries Series { get; }
    
    /// <summary>
    /// The date when the development for the Debian release started.
    /// </summary>
    public DateOnly Created { get; }
    
    /// <summary>
    /// The date when the Debian version was initially released.
    /// </summary>
    public DateOnly? Released { get; }
    
    /// <summary>
    /// The date when the (standard) <see href="https://wiki.debian.org/DebianStable">Stable Support</see>
    /// for this Debian release ends.
    /// </summary>
    public DateOnly? EndOfStandardSupport { get; }
    
    /// <summary>
    /// The date when the <see href="https://wiki.debian.org/LTS">Long Term Support (LTS)</see>
    /// for this Debian release ends.
    /// </summary>
    public DateOnly? EndOfLongTermSupport { get; }
    
    /// <summary>
    /// The date when the <see href="https://wiki.debian.org/LTS/Extended">Extended Long Term Support (ELTS)</see>
    /// for this Debian release ends.
    /// </summary>
    public DateOnly? EndOfExtendedLongTermSupport { get; }
    
    /// <summary>
    /// The date representing the end of life for the Debian release.
    /// </summary>
    /// <remarks>
    /// This is the latest date of <see cref="EndOfStandardSupport"/>, <see cref="EndOfLongTermSupport"/> and
    /// <see cref="EndOfExtendedLongTermSupport"/>.
    /// </remarks>
    public DateOnly? EndOfLife { get; }
    
    internal DebianRelease(
        bool isStable,
        string? version,
        string codename,
        string series,
        DateOnly created,
        DateOnly? released,
        DateOnly? endOfStandardSupport,
        DateOnly? endOfLongTermSupport,
        DateOnly? endOfExtendedLongTermSupport,
        DateOnly? endOfLife,
        string stringRepresentation)
    {
        IsStable = isStable;
        Version = version;
        Codename = codename;
        Series = DpkgSeries.Parse(series);
        Created = created;
        Released = released;
        EndOfStandardSupport = endOfStandardSupport;
        EndOfLongTermSupport = endOfLongTermSupport;
        EndOfExtendedLongTermSupport = endOfExtendedLongTermSupport;
        EndOfLife = endOfLife;
        _stringRepresentation = stringRepresentation;
    }

    /// <inheritdoc />
    public override string ToString() => _stringRepresentation;
}