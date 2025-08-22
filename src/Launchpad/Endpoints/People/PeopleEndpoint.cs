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

namespace Canonical.Launchpad.Endpoints.People;

[JsonConverter(typeof(EndpointJsonConverter<PeopleEndpoint>))]
public readonly record struct PeopleEndpoint(
    ApiRoot ApiRoot, 
    string Name) 
    : ILaunchpadEndpoint<PeopleEndpoint>
{
    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());
    internal StringBuilder BuildEndpointRoot() => ApiRoot.BuildEndpointRoot().Append("/~").Append(UrlEncode(Name));
    
    /// <inheritdoc />
    public static PeopleEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot.TrimEnd('/').Split<ApiRoot, PeopleEndpoint>(
            separator: '~',
            out var apiRoot,
            out var nameSlice);
        
        return apiRoot.People(name: UrlDecode(nameSlice.ToString()));
    }

    public PpaEndpoint Ppa(string name) => new(Owner: this, Name: name);
}
