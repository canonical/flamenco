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
using Canonical.Launchpad.Endpoints.People;
using static System.Web.HttpUtility;

namespace Canonical.Launchpad.Endpoints;

[JsonConverter(typeof(EndpointJsonConverter<ApiRoot>))]
public readonly record struct ApiRoot(
    ApiEntryPoint EntryPoint, 
    ApiVersion Version) 
    : ILaunchpadEndpoint<ApiRoot>
{
    /// <summary>
    /// Initializes the stable (1.0) production API root.
    /// </summary>
    public ApiRoot() : this(ApiEntryPoints.Production, ApiVersion.OnePointZero)
    {
    }
 
    /// <inheritdoc />
    public Uri EndpointRoot => new Uri(BuildEndpointRoot().ToString());

    internal StringBuilder BuildEndpointRoot() => new StringBuilder()
        .Append(EntryPoint.RootUri)
        .Append(Version.Identifier);
    
    /// <inheritdoc />
    public static ApiRoot ParseEndpointRoot(ReadOnlySpan<char> endpointRoot)
    {
        endpointRoot = endpointRoot.TrimEnd('/');
        int separatorIndex = endpointRoot.LastIndexOf('/');
        
        if (separatorIndex < 0)
        {
            throw new FormatException(message: $"'{endpointRoot}' is not a valid {nameof(ApiRoot)} uri.");
        }

        var versionSlice = endpointRoot[(separatorIndex + 1)..];
        var entryPointSlice = endpointRoot[..(separatorIndex + 1)];
        
        ApiEntryPoint? entryPoint = null;
        
        foreach (var apiEntryPoint in ApiEntryPoints.All)
        {
            if (entryPointSlice.SequenceEqual(apiEntryPoint.RootUri))
            {
                entryPoint = apiEntryPoint;
                break;
            }
        }

        entryPoint ??= new ApiEntryPoint(
            Name: "Custom Launchpad API Entry Point",
            RootUri: entryPointSlice.ToString(),
            Environment: LaunchpadEnvironment.Unknown);
        
        return new ApiRoot(
            EntryPoint: entryPoint.Value, 
            Version: new ApiVersion(UrlDecode(versionSlice.ToString())));
    }
    
    public DistroEndpoint Distribution(string name) => new (ApiRoot: this, Name: name);
    
    public PeopleEndpoint People(string name) => new (ApiRoot: this, Name: name);
}

public static class ApiRootExtensions
{
    public static ApiRoot With(this ApiEntryPoint entryPoint, ApiVersion version) => new ApiRoot(entryPoint, version);
}