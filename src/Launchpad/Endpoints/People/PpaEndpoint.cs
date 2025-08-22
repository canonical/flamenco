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
using Canonical.Launchpad.Endpoints.Distro;
using Canonical.Launchpad.Entities;
using Canonical.Launchpad.Exceptions;
using static System.Web.HttpUtility;

namespace Canonical.Launchpad.Endpoints.People;

[JsonConverter(typeof(EndpointJsonConverter<PpaEndpoint>))]
public readonly record struct PpaEndpoint(
    PeopleEndpoint Owner,
    string Name) 
    : ILaunchpadEndpoint<PpaEndpoint>
{
    private const string PpaUriSegment = "/+archive/ubuntu/";

    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => Owner.BuildEndpointRoot().Append(PpaUriSegment).Append(Name);
    
    /// <inheritdoc />
    public static PpaEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/').Split<PeopleEndpoint, PpaEndpoint>(
            separator: PpaUriSegment,
            out var people,
            out var nameSlice);

        return people.Ppa(name: nameSlice.ToString());
    }

    /// <summary>
    /// Endpoint to get a record with a specific identifier from the source package publishing history for this ppa.
    /// </summary>
    /// <param name="id">Identifier of the record.</param>
    /// <returns>
    /// The endpoint for the record with the identifier <paramref name="id"/> from the
    /// source package publishing history for this ppa.
    /// </returns>
    public PpaSourcePackagePublishingHistoryRecordEndpoint SourcePackagePublishingHistoryRecord(uint id) =>
        new (Ppa: this, Id: id);

    /// <summary>
    /// Endpoint to get a record with a specific identifier from the binary package publishing history for this ppa.
    /// </summary>
    /// <param name="id">Identifier of the record.</param>
    /// <returns>
    /// The endpoint for the record with the identifier <paramref name="id"/> from the
    /// binary package publishing history for this ppa.
    /// </returns>
    public PpaBinaryPackagePublishingHistoryRecordEndpoint BinaryPackagePublishingHistoryRecord(uint id) =>
        new (Ppa: this, Id: id);

    /// <summary>
    /// Gets history of published source packages related to this ppa.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to query the publishing history.
    /// </param>
    /// <param name="sourcePackageName">
    /// Only return records related to source packages with a name that matches the specified value.
    /// </param>
    /// <param name="exactNameMatch">
    /// <see langword="true"/> to only return records related to source packages with a name that matches the specified
    /// value of <paramref name="sourcePackageName"/> exactly; <see langword="false"/> to fuzzy match.
    /// </param>
    /// <param name="version">
    /// Only return records related to source packages with a version that matches the specified value.
    /// </param>
    /// <param name="component">
    /// Only return records related to the specified component.
    /// </param>
    /// <param name="pocket">
    /// Only return records related to the specified <see cref="Pocket"/>.
    /// </param>
    /// <param name="series">
    /// Only return records related to the specified distribution series.
    /// </param>
    /// <param name="status">
    /// Only return records which have the specified <see cref="PackagePublishingStatus"/>.
    /// </param>
    /// <param name="createdSinceDate">
    /// Only return records whose <see cref="PackagePublishingHistoryRecord.Created"/> date
    /// is greater than or equal to the specified date.
    /// </param>
    /// <param name="orderByCreationDate">
    /// <see langword="true"/> to order the returned records by <see cref="PackagePublishingHistoryRecord.Created"/>
    /// from newest to oldest. This is recommended for applications that need to catch up with publications since
    /// their last run. <see langword="false"/> to order the returned records by
    /// <see cref="PackagePublishingHistoryRecord.SourcePackageName"/> (lexicographically), then by
    /// <see cref="PackagePublishingHistoryRecord.SourcePackageVersion"/> (descending) and then by
    /// id (descending).
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
    /// <returns>
    /// A representation of the collection of package publishing records which are related to the query parameters.
    /// </returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task<FragmentedCollection<PpaSourcePackagePublishingHistoryRecord>> GetPublishedSourcesAsync(
        HttpClient httpClient,
        string? sourcePackageName = null,
        bool exactNameMatch = false,
        string? version = null,
        string? component = null,
        Pocket? pocket = null,
        DistroSeriesEndpoint? series = null,
        PackagePublishingStatus? status = null,
        DateTime? createdSinceDate = null,
        bool orderByCreationDate = false,
        uint offset = 0,
        uint fragmentSize = 0,
        CancellationToken cancellationToken = default)
    {
        var collectionLink = BuildEndpointRoot().Append("?ws.op=getPublishedSources");

        if (sourcePackageName is not null)
        {
            collectionLink.Append("&source_name=").Append(UrlEncode(sourcePackageName));
            if (exactNameMatch) collectionLink.Append("&exact_match=true");
        }

        if (series.HasValue)
        {
            var distroSeriesLink = series.Value.BuildEndpointRoot().ToString();
            collectionLink.Append("&distro_series=").Append(UrlEncode(distroSeriesLink));
        }

        if (createdSinceDate.HasValue)
        {
            var iso8601Date = createdSinceDate.Value.ToUniversalTime().ToString("O");
            collectionLink.Append("&created_since_date=").Append(UrlEncode(iso8601Date));
        }

        if (version is not null) collectionLink.Append("&version=").Append(UrlEncode(version));
        if (component is not null) collectionLink.Append("&component_name=").Append(UrlEncode(component));
        if (pocket.HasValue) collectionLink.Append("&pocket=").Append(pocket.Value.ToString());
        if (status.HasValue) collectionLink.Append("&status=").Append(status.Value.ToString());
        if (orderByCreationDate) collectionLink.Append("&order_by_date=true");
        if (offset > 0) collectionLink.Append("&ws.start=").Append(offset);
        if (fragmentSize > 0) collectionLink.Append("&ws.size=").Append(fragmentSize);

        return FragmentedCollection<PpaSourcePackagePublishingHistoryRecord>.FetchAsync(
            collectionLink: new Uri(collectionLink.ToString()),
            httpClient, cancellationToken);
    }

    /// <summary>
    /// Gets history of published binary packages related to this ppa.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to query the publishing history.
    /// </param>
    /// <param name="binaryPackageName">
    /// Only return records related to binary packages with a name that matches the specified value.
    /// </param>
    /// <param name="component">
    /// Only return records related to the specified component.
    /// </param>
    /// <param name="createdSinceDate">
    /// Only return records whose <see cref="PackagePublishingHistoryRecord.Created"/> date
    /// is greater than or equal to the specified date.
    /// </param>
    /// <param name="seriesArchitecture"></param>
    /// <param name="exactNameMatch"></param>
    /// <param name="orderByCreationDate">
    /// <see langword="true"/> to order the returned records by <see cref="PackagePublishingHistoryRecord.Created"/>
    /// from newest to oldest. This is recommended for applications that need to catch up with publications since
    /// their last run.
    /// </param>
    /// <param name="ordered">
    /// <see langword="true"/> to return ordered results (default).
    /// <see langword="false"/> will return results more quickly.
    /// </param>
    /// <param name="pocket">
    /// Only return records related to the specified <see cref="Pocket"/>.
    /// </param>
    /// <param name="status">
    /// Only return records which have the specified <see cref="PackagePublishingStatus"/>.
    /// </param>
    /// <param name="version">
    /// Only return records related to binary packages with a version that matches the specified value.
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
    /// <returns>
    /// A representation of the collection of package publishing records which are related to the query parameters.
    /// </returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task<FragmentedCollection<PpaBinaryPackagePublishingHistoryRecord>> GetPublishedBinariesAsync(
        HttpClient httpClient,
        string? binaryPackageName = null,
        string? component = null,
        DateTime? createdSinceDate = null,
        DistroSeriesArchEndpoint? seriesArchitecture = null,
        bool exactNameMatch = false,
        bool orderByCreationDate = false,
        bool ordered = true,
        Pocket? pocket = null,
        PackagePublishingStatus? status = null,
        string? version = null,
        uint offset = 0,
        uint fragmentSize = 0,
        CancellationToken cancellationToken = default)
    {
        var collectionLink = BuildEndpointRoot().Append("?ws.op=getPublishedSources");

        if (binaryPackageName is not null)
        {
            collectionLink.Append("&binary_name=").Append(UrlEncode(binaryPackageName));
            if (exactNameMatch) collectionLink.Append("&exact_match=true");
        }

        if (seriesArchitecture.HasValue)
        {
            var distroSeriesArchLink = seriesArchitecture.Value.BuildEndpointRoot().ToString();
            collectionLink.Append("&distro_arch_series=").Append(UrlEncode(distroSeriesArchLink));
        }

        if (createdSinceDate.HasValue)
        {
            var iso8601Date = createdSinceDate.Value.ToUniversalTime().ToString("O");
            collectionLink.Append("&created_since_date=").Append(UrlEncode(iso8601Date));
        }

        if (version is not null) collectionLink.Append("&version=").Append(UrlEncode(version));
        if (component is not null) collectionLink.Append("&component_name=").Append(UrlEncode(component));
        if (pocket.HasValue) collectionLink.Append("&pocket=").Append(pocket.Value.ToString());
        if (status.HasValue) collectionLink.Append("&status=").Append(status.Value.ToString());
        if (orderByCreationDate) collectionLink.Append("&order_by_date=true");
        if (!ordered) collectionLink.Append("&ordered=false");
        if (offset > 0) collectionLink.Append("&ws.start=").Append(offset);
        if (fragmentSize > 0) collectionLink.Append("&ws.size=").Append(fragmentSize);

        return FragmentedCollection<PpaBinaryPackagePublishingHistoryRecord>.FetchAsync(
            collectionLink: new Uri(collectionLink.ToString()),
            httpClient, cancellationToken);
    }
}
