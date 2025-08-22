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

namespace Flamenco.Distro.Services.Abstractions;

/// <summary>
/// An immutable representation of the release state of dpkg source/binary package.
/// </summary>
/// <param name="Package">The name of the package that is released.</param>
/// <param name="Version">The version of the package that is released.</param>
/// <param name="Architecture">The architecture of the package that is released.</param>
/// <param name="ArchiveSection">The section of the archive where this package is released to.</param>
public record DpkgPackageReleaseState(
    DpkgName Package, 
    DpkgVersion Version, 
    DpkgArchitecture Architecture, 
    DpkgArchiveSection ArchiveSection,
    bool IsPendingOrProposed)
{
    /// <summary>
    /// Weather this release state refers to a source package.
    /// </summary>
    public bool IsSourcePackage => Architecture == DpkgArchitecture.Source;
    
    /// <summary>
    /// Weather this release state refers to a binary package.
    /// </summary>
    public bool IsBinaryPackage => Architecture != DpkgArchitecture.Source;
}
