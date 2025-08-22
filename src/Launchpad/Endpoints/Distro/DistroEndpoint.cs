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
using static System.Web.HttpUtility;

namespace Canonical.Launchpad.Endpoints.Distro;

/// <summary>
/// Represents the API endpoint for a distribution on Launchpad.
/// </summary>
/// <param name="ApiRoot">The root API endpoint of Launchpad.</param>
/// <param name="Name">The name of the distribution.</param>
[JsonConverter(typeof(EndpointJsonConverter<DistroEndpoint>))]
public readonly record struct DistroEndpoint(
    ApiRoot ApiRoot, 
    string Name) 
    : ILaunchpadEndpoint<DistroEndpoint>
{
    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => ApiRoot.BuildEndpointRoot().Append('/').Append(UrlEncode(Name));
    
    /// <inheritdoc />
    public static DistroEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/').Split<ApiRoot, DistroEndpoint>(
            separator: '/',
            out var apiRoot,
            out var nameSlice);
        
        return apiRoot.Distribution(name: UrlDecode(nameSlice.ToString()));
    }
    
    /// <summary>
    /// Endpoint to get a series with a specific name of this distribution.
    /// </summary>
    /// <param name="name">Name of the series.</param>
    /// <returns>The endpoint for the series with the name <paramref name="name"/> of this distribution.</returns>
    public DistroSeriesEndpoint Series(string name) => 
        new(Distribution: this, Name: name);

    /// <summary>
    /// Endpoint to get an archive with a specific name for this distribution.
    /// </summary>
    /// <param name="name">Name of the archive.</param>
    /// <returns>The endpoint for the archive with the name <paramref name="name"/> for this distribution.</returns>
    public DistroArchiveEndpoint Archive(string name) =>
        new DistroArchiveEndpoint(Distribution: this, Name: name);
}