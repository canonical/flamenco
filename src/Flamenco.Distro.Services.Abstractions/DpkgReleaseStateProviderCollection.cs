// This file is part of Flamenco
// Copyright 2025 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;

namespace Flamenco.Distro.Services.Abstractions;

public class DpkgReleaseStateProviderCollection : IDpkgReleaseStateProvider
{
    public required IImmutableList<IDpkgReleaseStateProvider> DpkgReleaseStateProviders { get; init; }

    /// <inheritdoc />
    public async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        DpkgReleaseStateQueryOptions options,
        CancellationToken cancellationToken)
    {
        var queryTasks = new Task<Result<IImmutableList<DpkgPackageReleaseState>>>[DpkgReleaseStateProviders.Count];

        for (int i = 0; i < DpkgReleaseStateProviders.Count; i++)
        {
            queryTasks[i] = DpkgReleaseStateProviders[i].QueryAsync(options, cancellationToken);
        }

        await Task.WhenAll(queryTasks).ConfigureAwait(false);

        var combinedResult = Result.Success;
        IImmutableList<DpkgPackageReleaseState> combinedReleaseStates = ImmutableList<DpkgPackageReleaseState>.Empty;

        foreach (var queryTask in queryTasks)
        {
            var result = await queryTask.ConfigureAwait(false);
            combinedResult = combinedResult.Merge(result);

            if (result.TryGetValue(out var releaseStates))
            {
                combinedReleaseStates = combinedReleaseStates.AddRange(releaseStates);
            }
        }

        return combinedResult.WithValue(combinedReleaseStates);
    }
}
