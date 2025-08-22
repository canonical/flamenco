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

namespace Canonical.Launchpad.Endpoints.Distro;

[JsonConverter(typeof(EndpointJsonConverter<DistroSeriesPackageUploadEndpoint>))]
public readonly record struct DistroSeriesPackageUploadEndpoint(
    DistroSeriesEndpoint Series, 
    uint Id) 
    : ILaunchpadEndpoint<DistroSeriesPackageUploadEndpoint>
{
    private const string UploadUriSegment = "/+upload/";
    
    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => Series.BuildEndpointRoot().Append(UploadUriSegment).Append(Id);  
    
    /// <inheritdoc />
    public static DistroSeriesPackageUploadEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/').Split<DistroSeriesEndpoint, DistroSeriesPackageUploadEndpoint>(
            separator: UploadUriSegment,
            out var series,
            out var idSlice);
        
        if (!uint.TryParse(idSlice, out uint id))
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is no valid {nameof(DistroSeriesPackageUploadEndpoint)} link. " +
                $"The id '{idSlice}' is not an unsigned integer.");
        }
        
        return series.PackageUpload(id);
    }
}