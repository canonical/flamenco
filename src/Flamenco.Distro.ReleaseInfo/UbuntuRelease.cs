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
/// An immutable record of details about an Ubuntu release. 
/// </summary>
public record UbuntuRelease
{
    private readonly string _stringRepresentation;
    
    /// <summary>
    /// The version identifier of Ubuntu release.
    /// </summary>
    /// <example>
    /// <c>"24.04"</c>
    /// </example>
    public string Version { get; }

    /// <summary>
    /// Determines whether the Ubuntu release is a Long Term Support (LTS) release.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the Ubuntu release is an LTS release; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsLts { get; }

    /// <summary>
    /// The codename of the Ubuntu release.
    /// </summary>
    /// <example>
    /// <c>"Noble Numbat"</c>
    /// </example>
    public string Codename { get; }

    /// <summary>
    /// Represents the series identifier of an Ubuntu release.
    /// </summary>
    /// <example>
    /// <c>noble</c>
    /// </example>
    public DpkgSeries Series { get; }

    /// <summary>
    /// The date when the development for the Ubuntu release started.
    /// </summary>
    public DateOnly Created { get; }
    
    /// <summary>
    /// The date when the Ubuntu version was initially released.
    /// </summary>
    public DateOnly Released { get; }
    
    /// <summary>
    /// The date when the standard support for the Ubuntu version ends.
    /// </summary>
    /// <remarks>
    /// If <see cref="EndOfServerStandardSupport"/> is not <see langword="null"/> than this
    /// date just represents when the standard support for the Ubuntu desktop version ends.
    /// This is just the case for older Ubuntu releases.
    /// </remarks>
    public DateOnly EndOfStandardSupport { get; }
    
    /// <summary>
    /// If this is not <see langword="null"/>, it represents the end of standard support date
    /// for Ubuntu server in older versions; otherwise Ubuntu server has the same standard
    /// support length as Ubuntu desktop (<see cref="EndOfStandardSupport"/>).
    /// </summary>
    /// <remarks>
    /// Only older Ubuntu releases have a difference between standard support length for
    /// Ubuntu desktop and Ubuntu server.
    /// </remarks>
    public DateOnly? EndOfServerStandardSupport { get; }

    /// <summary>
    /// The date when the expanded security maintenance (ESM) for the Ubuntu release ends.
    /// </summary>
    /// <remarks>
    /// Only newer Ubuntu LTS releases do have ESM. <see langword="null"/> will be returned for releases without ESM.
    /// </remarks>
    public DateOnly? EndOfExpandedSecurityMaintenance { get; }

    /// <summary>
    /// The date representing the end of life for the Ubuntu release.
    /// </summary>
    /// <remarks>
    /// This is the latest date of <see cref="EndOfStandardSupport"/>, <see cref="EndOfServerStandardSupport"/> and
    /// <see cref="EndOfExpandedSecurityMaintenance"/> that is not null.
    /// </remarks>
    public DateOnly EndOfLife { get; }

    internal UbuntuRelease(
        string version,
        bool isLts,
        string codename,
        string series,
        DateOnly created,
        DateOnly released,
        DateOnly endOfStandardSupport,
        DateOnly? endOfServerStandardSupport,
        DateOnly? endOfExpandedSecurityMaintenance,
        DateOnly endOfLife,
        string stringRepresentation)
    {
        Version = version;
        IsLts = isLts;
        Codename = codename;
        Series = DpkgSeries.Parse(series);
        Created = created;
        Released = released;
        EndOfStandardSupport = endOfStandardSupport;
        EndOfServerStandardSupport = endOfServerStandardSupport;
        EndOfExpandedSecurityMaintenance = endOfExpandedSecurityMaintenance;
        EndOfLife = endOfLife;
        _stringRepresentation = stringRepresentation;
    }

    /// <inheritdoc />
    public override string ToString() => _stringRepresentation;
}