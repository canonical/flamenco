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
/// The state a <see cref="PackageUpload"/> is in.
/// </summary>
public enum PackageUploadStatus
{
    /// <summary>
    /// Upload of a new package that is not in the archive yet. The upload is not triaged yet. 
    /// </summary>
    New,
    
    /// <summary>
    /// Upload of a new version of a previously existing package in the archive. The upload is not triaged yet.
    /// </summary>
    Unapproved,
    
    /// <summary>
    /// Upload has been accepted. The binaries will now start building and the resulting builds have to be verified.
    /// </summary>
    Accepted,
    
    /// <summary>
    /// Upload is done. The upload is in the requested location.
    /// </summary>
    Done,
    
    /// <summary>
    /// The upload was rejected.
    /// </summary>
    Rejected,
}