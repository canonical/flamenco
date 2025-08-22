// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Text;
using System.Text.Json.Serialization;
using Canonical.Launchpad.Entities;
using static System.Web.HttpUtility;

namespace Canonical.Launchpad.Endpoints.Distro;

[JsonConverter(typeof(EndpointJsonConverter<DistroSeriesEndpoint>))]
public readonly record struct DistroSeriesEndpoint(
    DistroEndpoint Distribution, 
    string Name) 
    : ILaunchpadEndpoint<DistroSeriesEndpoint>
{
    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => Distribution.BuildEndpointRoot().Append('/').Append(UrlEncode(Name));
    
    /// <inheritdoc />
    public static DistroSeriesEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/').Split<DistroEndpoint, DistroSeriesEndpoint>(
            separator: '/',
            out var distribution,
            out var nameSlice);
        
        return distribution.Series(name: UrlDecode(nameSlice.ToString()));
    }

    public DistroSeriesPackageUploadEndpoint PackageUpload(uint id) => new(Series: this, Id: id);
    
    public DistroSeriesArchEndpoint Architecture(string name) => new(Series: this, Name: name);

    /// <summary>
    /// Get package upload records for this distribution series.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to query the package upload records.
    /// </param>
    /// <param name="archive">
    /// Return only records for this archive.
    /// </param>
    /// <param name="createdSinceDate">
    /// Only return records whose <see cref="PackageUpload.Created"/> date
    /// is greater than or equal to the specified date.
    /// </param>
    /// <param name="customFileType">
    /// Return only records with custom files of this type.
    /// </param>
    /// <param name="exactNameMatch">
    /// <see langword="true"/> to only return records with a name that matches the specified value of
    /// <paramref name="name"/> exactly; <see langword="false"/> to fuzzy match.
    /// </param>
    /// <param name="name">
    /// Only return records with this package or file name.
    /// </param>
    /// <param name="pocket">
    /// Only return records related to the specified <see cref="Pocket"/>.
    /// </param>
    /// <param name="status">
    /// Only return records which have the specified <see cref="PackageUploadStatus"/>.
    /// </param>
    /// <param name="version">
    /// Only return records which have the specified package version.
    /// </param>
    /// <param name="offset">
    /// How many records should be skipped from the beginning of the collection for the initial
    /// <see cref="CollectionFragment{TEntry}"/> that will be fetched.  
    /// </param>
    /// <param name="fragmentSize">
    /// The maximum amount of records contained in the initial <see cref="CollectionFragment{TEntry}"/> that
    /// will be fetched.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns></returns>
    public Task<FragmentedCollection<PackageUpload>> GetPackageUploadsAsync(
        HttpClient httpClient,
        DistroArchiveEndpoint? archive = null,
        DateTime? createdSinceDate = null,
        CustomFileType? customFileType = null,
        bool exactNameMatch = false,
        string? name = null,
        Pocket? pocket = null,
        PackageUploadStatus? status = null,
        string? version = null,
        uint offset = 0,
        uint fragmentSize = 0,
        CancellationToken cancellationToken = default)
    {
        var collectionLink = BuildEndpointRoot().Append("?ws.op=getPackageUploads");
        
        if (name is not null)
        {
            collectionLink.Append("&name=").Append(UrlEncode(name));
            if (exactNameMatch) collectionLink.Append("&exact_match=true");
        }
        
        if (archive.HasValue)
        {
            var archiveLink = archive.Value.BuildEndpointRoot().ToString();
            collectionLink.Append("&archive=").Append(UrlEncode(archiveLink));
        }
        
        if (createdSinceDate.HasValue)
        {
            var iso8601Date = createdSinceDate.Value.ToUniversalTime().ToString("O");
            collectionLink.Append("&created_since_date=").Append(UrlEncode(iso8601Date));
        }
        
        if (customFileType is not null) collectionLink.Append("&custom_type=").Append(customFileType.Value); 
        if (pocket.HasValue) collectionLink.Append("&pocket=").Append(pocket.Value.ToString());
        if (status.HasValue) collectionLink.Append("&status=").Append(status.Value.ToString());
        if (version is not null) collectionLink.Append("&version=").Append(UrlEncode(version));
        if (offset > 0) collectionLink.Append("&ws.start=").Append(offset);
        if (fragmentSize > 0) collectionLink.Append("&ws.size=").Append(fragmentSize);
        
        return FragmentedCollection<PackageUpload>.FetchAsync(
            collectionLink: new Uri(collectionLink.ToString()), 
            httpClient, cancellationToken);
    }
}