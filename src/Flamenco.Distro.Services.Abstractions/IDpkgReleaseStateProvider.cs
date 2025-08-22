// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.Services.Abstractions;

/// <summary>
/// Represents a service that can provide details about the release state of dpkg packages. 
/// </summary>
public interface IDpkgReleaseStateProvider
{
    /// <summary>
    /// Query details about the release state of dpkg packages.
    /// </summary>
    /// <param name="options">The parameter that specify which details the query should return.</param>
    /// <param name="cancellationToken">A token that allows to cancel the operation, before it is finished.</param>
    /// <returns>A result that may contain all release states relevant to the query <paramref name="options"/>.</returns>
    public Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        DpkgReleaseStateQueryOptions options,
        CancellationToken cancellationToken);
}

/// <summary>
/// Immutable representation of parameters for a dpkg release state provider query.  
/// </summary>
public readonly record struct DpkgReleaseStateQueryOptions()
{
    /// <summary>
    /// The packages which will be queried
    /// </summary>
    public required IImmutableList<DpkgName> PackageNames { get; init; } = ImmutableList<DpkgName>.Empty;

    /// <summary>
    /// The architectures of the results have to be included in this list.
    /// If the list is empty, every architecture is allowed.
    /// </summary>
    public IImmutableList<DpkgArchitecture> Architectures { get; init; } = ImmutableList<DpkgArchitecture>.Empty;
    
    /// <summary>
    /// The components of the results have to be included in this list.
    /// If the list is empty, every component is allowed.
    /// </summary>
    public IImmutableList<DpkgComponent> Components { get; init; } = ImmutableList<DpkgComponent>.Empty;
    
    /// <summary>
    /// The suites of the results have to be included in this list.
    /// If the list is empty, every suite is allowed.
    /// </summary>
    public IImmutableList<DpkgSuite> Suites { get; init; } = ImmutableList<DpkgSuite>.Empty;

    /// <summary>
    /// <see langword="true"/> if the binary packages of source packages should be included in the query results;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IncludeBinaryPackagesOfSourcePackages { get; init; } = false;
}
