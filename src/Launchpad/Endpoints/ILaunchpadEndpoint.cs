// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Canonical.Launchpad.Endpoints;

/// <summary>
/// Represents a Launchpad API endpoint to send requests to. 
/// </summary>
public interface ILaunchpadEndpoint<TEndpoint>
{
    /// <summary>
    /// Get the root Uri of this endpoint.
    /// </summary>
    public Uri EndpointRoot { get; }

    /// <summary>
    /// Parses the root uri of the endpoint represented as a string.
    /// </summary>
    /// <param name="endpointRoot">The root uri of the endpoint represented as a string.</param>
    /// <returns>The parsed <see cref="TEndpoint"/>.</returns>
    public static abstract TEndpoint ParseEndpointRoot(ReadOnlySpan<char> endpointRoot);
}