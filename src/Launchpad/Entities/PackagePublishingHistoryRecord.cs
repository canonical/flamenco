// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Text.Json.Serialization;
using Canonical.Launchpad.Endpoints.Distro;
using Canonical.Launchpad.Endpoints.People;

namespace Canonical.Launchpad.Entities;

/// <summary>
/// A record in the publishing history of a package to an archive.
/// </summary>
/// <remarks>
/// Contains the common properties between
/// <see cref="PpaSourcePackagePublishingHistoryRecord"/> and
/// <see cref="PpaBinaryPackagePublishingHistoryRecord"/>.
/// </remarks>
public abstract record PackagePublishingHistoryRecord
{
    /// <summary>
    /// The archive component name into which this entry is published.
    /// </summary>
    [JsonRequired]
    public required string ComponentName { get; init; }

    /// <summary>
    /// A link to the person who created this publication.
    /// </summary>
    public PeopleEndpoint? CreatorLink { get; init; } = null;

    /// <summary>
    /// Text representation of the record.
    /// </summary>
    [JsonRequired] 
    public required string DisplayName { get; init; }

    /// <summary>
    /// The pocket into which this entry is published.
    /// </summary>
    [JsonRequired] 
    public required Pocket Pocket { get; init; }

    /// <summary>
    /// The status of this publishing record.
    /// </summary>
    [JsonRequired] 
    public required PackagePublishingStatus Status { get; init; }

    /// <summary>
    /// The archive section name which this entry is belongs to.
    /// </summary>
    [JsonRequired, JsonPropertyName("section_name")]
    public required string Section { get; init; }

    /// <summary>
    /// The name of the source package.
    /// </summary>
    [JsonRequired] 
    public required string SourcePackageName { get; init; }
    
    /// <summary>
    /// The version of the source package.
    /// </summary>
    [JsonRequired] 
    public required string SourcePackageVersion { get; init; }
    
    /// <summary>
    /// Reason why this publication is going to be removed.
    /// </summary>
    public string? RemovalComment { get; init; } = null;

    /// <summary>
    /// A link to the person responsible for the removal. 
    /// </summary>
    public PeopleEndpoint? RemovedByLink { get; init; } = null;
    
    /// <summary>
    /// The date on which this record was created.
    /// </summary>
    [JsonRequired, JsonPropertyName("date_created")]
    public required DateTimeOffset Created { get; init; }
    
    /// <summary>
    /// The date on which this record was set as pending removal. 
    /// </summary>
    [JsonPropertyName("date_made_pending")]
    public DateTimeOffset? MadePending { get; init; } = null;

    /// <summary>
    /// The date on which this record was published. 
    /// </summary>
    [JsonPropertyName("date_published")] 
    public DateTimeOffset? Published { get; init; } = null;
    
    /// <summary>
    /// The date on which this record was removed from the published set. 
    /// </summary>
    [JsonPropertyName("date_removed")] 
    public DateTimeOffset? Removed { get; init; } = null;
    
    /// <summary>
    /// The date on which this record was marked superseded.
    /// </summary>
    [JsonPropertyName("date_superseded")] 
    public DateTimeOffset? Superseded { get; init; } = null;

    /// <summary>
    /// The date on which this record is scheduled for deletion.
    /// </summary>
    [JsonPropertyName("scheduled_deletion_date")]
    public DateTimeOffset? ScheduledDeletion { get; init; } = null;

    /// <inheritdoc cref="ILaunchpadEntity{TEndpoint}.HttpEntityTag" />
    [JsonRequired, JsonPropertyName(name: "http_etag")]
    public required string HttpEntityTag { get; init; }
}

/// <summary>
/// A record in the publishing history of a source package to an archive.
/// </summary>
/// <seealso href="https://api.launchpad.net/1.0/#source_package_publishing_history">Launchpad API Doc</seealso>
public record PpaSourcePackagePublishingHistoryRecord : 
    PackagePublishingHistoryRecord, 
    ILaunchpadEntity<PpaSourcePackagePublishingHistoryRecordEndpoint>
{
    /// <summary>
    /// A link to the distro series the source package is being published into.
    /// </summary>
    [JsonRequired, JsonPropertyName("distro_series_link")]
    public required DistroSeriesEndpoint DistroSeriesLink { get; init; }
    
    /// <summary>
    /// A link to the person who created the source package.
    /// </summary>
    [JsonRequired]
    public required PeopleEndpoint PackageCreatorLink { get; init; }
    
    /// <summary>
    /// A link to the person who maintains the source package.
    /// </summary>
    [JsonRequired]
    public required PeopleEndpoint PackageMaintainerLink { get; init; }
    
    /// <summary>
    /// A link to the person who signed the source package.
    /// </summary>
    [JsonRequired]
    public required PeopleEndpoint PackageSignerLink { get; init; }
    
    /// <summary>
    /// A link to the package upload that caused the creation of this publication.
    /// </summary>
    [JsonPropertyName("packageupload_link")]
    public required DistroSeriesPackageUploadEndpoint? PackageUploadLink { get; init; }
    
    /// <inheritdoc />
    [JsonRequired] 
    public required PpaSourcePackagePublishingHistoryRecordEndpoint SelfLink { get; init; }
}

/// <summary>
/// A record in the publishing history of a binary package to an archive.
/// </summary>
/// <seealso href="https://api.launchpad.net/1.0/#binary_package_publishing_history">Launchpad API Doc</seealso>
public record PpaBinaryPackagePublishingHistoryRecord : 
    PackagePublishingHistoryRecord,
    ILaunchpadEntity<PpaBinaryPackagePublishingHistoryRecordEndpoint>
{
    /// <summary>
    /// Indicates if the package is architecture specific. 
    /// </summary>
    [JsonRequired]
    public required bool ArchitectureSpecific { get; init; }
    
    /// <summary>
    /// The name of the binary package.
    /// </summary>
    [JsonRequired]
    public required string BinaryPackageName { get; init; }
    
    /// <summary>
    /// The version of the binary package.
    /// </summary>
    [JsonRequired]
    public required string BinaryPackageVersion { get; init; }
    
    // TODO: build_link
    
    /// <summary>
    /// The distribution series architecture being published into.
    /// </summary>
    [JsonRequired, JsonPropertyName("distro_arch_series_link")]
    public required DistroSeriesEndpoint DistroSeriesArchLink { get; init; }

    /// <summary>
    /// The percentage of users for whom this package should be recommended to.
    /// <see langword="null"/> to publish the update for everyone. 
    /// </summary>
    public int? PhasedUpdatePercentage { get; init; } = null;

    /// <summary>
    /// The name of the priority for the binary package.
    /// </summary>
    [JsonRequired]
    public required string PriorityName { get; init; }

    /// <inheritdoc />
    [JsonRequired] 
    public required PpaBinaryPackagePublishingHistoryRecordEndpoint SelfLink { get; init; }
}
