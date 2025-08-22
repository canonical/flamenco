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
/// Pocket of the Ubuntu archive.
/// </summary>
/// <seealso href="https://canonical-ubuntu-packaging-guide.readthedocs-hosted.com/en/latest/explanation/archive/#pockets"/>
public enum Pocket
{
    /// <summary>
    /// This pocket contains the packages that an Ubuntu series was initially released with.
    /// After the initial release of an Ubuntu series, the packages in this pocket are not updated
    /// (not even for security-related fixes).
    /// </summary>
    Release,
    
    /// <summary>
    /// This pocket contains security-related updates to packages in the release pocket.
    /// </summary>
    Security,
    
    /// <summary>
    /// This pocket contains non-security-related updates to packages in the release pocket.
    /// </summary>
    Updates,
    
    /// <summary>
    /// This pocket is a staging environment the Ubuntu community can opt into, to verify the stability of any updates
    /// before they get deployed to a broader range of consumers.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// Before the initial release of an Ubuntu series, this pocket contains non-security-related updates
    /// to packages in the release pocket before they get uploaded to the release pocket.
    /// </item>
    /// <item>
    /// After the initial release of an Ubuntu series, this pocket contains non-security-related updates to packages
    /// in the release pocket before they get uploaded to the updates pocket.
    /// </item>
    /// </list>
    /// </remarks>
    Proposed,
    
    /// <summary>
    /// This pocket contains packages the Ubuntu series was initially NOT released with.
    /// </summary>
    Backports,
}