// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Canonical.Launchpad.Entities;

/// <summary>
/// The state of a <see cref="PpaSourcePackagePublishingHistoryRecord"/> or
/// <see cref="PpaBinaryPackagePublishingHistoryRecord"/>
/// </summary>
public enum PackagePublishingStatus
{
    /// <summary>
    /// The package is in the process of being published. 
    /// </summary>
    Pending,
    
    /// <summary>
    /// The package is published.
    /// </summary>
    Published,
    
    /// <summary>
    /// A package with a newer version superseded this package.
    /// </summary>
    Superseded,
    
    /// <summary>
    /// The package was deleted.
    /// </summary>
    Deleted,
    
    /// <summary>
    /// The package is obsolete.
    /// </summary>
    Obsolete,
}
