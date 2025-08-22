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
using Canonical.Launchpad.Exceptions;

namespace Canonical.Launchpad.Endpoints.People;

/// <summary>
/// Represents the API endpoint for a record from the source package publishing history for a PPA on Launchpad.
/// </summary>
[JsonConverter(typeof(EndpointJsonConverter<PpaBinaryPackagePublishingHistoryRecordEndpoint>))]
public readonly partial record struct PpaBinaryPackagePublishingHistoryRecordEndpoint(
    PpaEndpoint Ppa,
    uint Id) 
    : ILaunchpadEndpoint<PpaBinaryPackagePublishingHistoryRecordEndpoint>
{
    private const string BinaryPubSegment = "/+binarypub/";

    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => Ppa.BuildEndpointRoot().Append(BinaryPubSegment).Append(Id);
    
    /// <inheritdoc />
    public static PpaBinaryPackagePublishingHistoryRecordEndpoint ParseEndpointRoot(
        ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/')
            .Split<PpaEndpoint, PpaSourcePackagePublishingHistoryRecordEndpoint>(
            separator: BinaryPubSegment,
            out var ppa,
            out var idSlice);
        
        if (!uint.TryParse(idSlice, out uint id))
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is no valid {nameof(PpaBinaryPackagePublishingHistoryRecordEndpoint)} " +
                $"link. The id '{idSlice}' is not an unsigned integer.");
        }
        
        return ppa.BinaryPackagePublishingHistoryRecord(id);
    }
    
    /// <summary>
    /// Get the binary package publishing history record asynchronously using an HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to query the record.</param>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>An entity that represents the binary package publishing history record of this endpoint.</returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public ValueTask<PpaBinaryPackagePublishingHistoryRecord> GetAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        return httpClient.GetAndParseJsonFromLaunchpadAsync<PpaBinaryPackagePublishingHistoryRecord>(
            uri: EndpointRoot, cancellationToken);
    }
}
