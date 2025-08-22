// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Canonical.Launchpad.Endpoints.Distro;

namespace Canonical.Launchpad.Entities;

/// <summary>
/// A queue item for the archive uploader.
/// </summary>
/// <seealso href="https://api.launchpad.net/1.0/#package_upload">Launchpad API Doc</seealso>
public record PackageUpload : ILaunchpadEntity<DistroSeriesPackageUploadEndpoint>
{
    /// <summary>
    /// The archive for this upload.
    /// </summary>
    [JsonRequired]
    public required DistroArchiveEndpoint ArchiveLink { get; init; }

    /// <summary>
    /// The archive from which this package was copied, if any.
    /// </summary>
    public DistroArchiveEndpoint? CopySourceArchiveLink { get; init; } = null;
    
    /// <summary>
    /// Librarian URLs for all the custom files attached to this upload.
    /// </summary>
    public IImmutableList<Uri> CustomFileUrls { get; init; } = ImmutableList<Uri>.Empty;
    
    /// <summary>
    /// The date this package upload was done.
    /// </summary>
    [JsonRequired, JsonPropertyName("date_created")]
    public required DateTimeOffset Created { get; init; }

    /// <summary>
    /// Architectures related to this item intended to be read by humans.
    /// </summary>
    [JsonRequired, JsonPropertyName("display_arches")]
    public required string DisplayArchitectures { get; init; }
    
    /// <summary>
    /// The display name for this queue item intended to be read by humans.
    /// </summary>
    [JsonRequired]
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// The source package version intended to be read by humans.
    /// </summary>
    [JsonRequired]
    public required string DisplayVersion { get; init; }
    
    /// <summary>
    /// The name the uploaded package.
    /// </summary>
    [JsonRequired, JsonPropertyName("package_name")]
    public required string PackageName { get; init; }
    
    /// <summary>
    /// The name the uploaded package.
    /// </summary>
    [JsonRequired, JsonPropertyName("package_version")]
    public required string PackageVersion { get; init; }
    
    /// <summary>
    /// A link to the distribution series targeted by this upload.
    /// </summary>
    [JsonRequired, JsonPropertyName("distroseries_link")]
    public required DistroSeriesEndpoint DistroSeriesLink { get; init; }
    
    /// <inheritdoc />
    [JsonRequired, JsonPropertyName("http_etag")] 
    public required string HttpEntityTag { get; init; }
    
    /// <summary>
    /// Identifier of this upload.
    /// </summary>
    [JsonRequired]
    public required int Id { get; init; }
    
    /// <summary>
    /// The component targeted by this upload.
    /// </summary>
    [JsonRequired, JsonPropertyName("component_name")]
    public required string ComponentName { get; init; }
    
    /// <summary>
    /// The pocket targeted by this upload.
    /// </summary>
    [JsonRequired]
    public required Pocket Pocket { get; init; }
    
    /// <inheritdoc />
    [JsonRequired]
    public required DistroSeriesPackageUploadEndpoint SelfLink { get; init; }
    
    /// <summary>
    /// The status of this upload.
    /// </summary>
    [JsonRequired]
    public required PackageUploadStatus Status { get; init; }
}
