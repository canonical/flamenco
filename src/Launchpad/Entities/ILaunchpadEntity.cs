// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using Canonical.Launchpad.Endpoints;

namespace Canonical.Launchpad.Entities;

public interface ILaunchpadEntity<TEndpoint> where TEndpoint : ILaunchpadEndpoint<TEndpoint> 
{
    /// <summary>
    /// A self-referential link to this resource. 
    /// </summary>
    TEndpoint SelfLink { get; }
    
    /// <summary>
    /// The value of the HTTP ETag for this resource. 
    /// </summary>
    string HttpEntityTag { get; }
}