using System.Collections.Immutable;
using System.Diagnostics;
using Canonical.Launchpad;
using Canonical.Launchpad.Endpoints;
using Canonical.Launchpad.Entities;
using Flamenco.Distro.ReleaseInfo;
using Flamenco.Distro.Services.Abstractions;
using Flamenco.Packaging.Dpkg;
using PackagePublishingStatus = Canonical.Launchpad.Entities.PackagePublishingStatus;
using Pocket = Canonical.Launchpad.Entities.Pocket;

namespace Flamenco.Distro.Services.Launchpad.ReleaseStateProviders;

public class LaunchpadPpaDpkgReleaseStateProvider(IHttpClientFactory httpClientFactory) : IDpkgReleaseStateProvider
{
    public required string PpaOwner { get; init; }

    public required string PpaName { get; init; }

    public async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        DpkgReleaseStateQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options.IncludeBinaryPackagesOfSourcePackages ||
            options.Architectures is not [] and not [{ Identifier: "source" }])
        {
            throw new NotImplementedException();
        }

        if (options.PackageNames is [])
        {
            return Result.Success.WithValue<IImmutableList<DpkgPackageReleaseState>>(ImmutableList<DpkgPackageReleaseState>.Empty);
        }

        return await Result.Success
            .Then(GetHttpClient)
            .Then(QueryReleaseStates)
            .ConfigureAwait(false);

        async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryReleaseStates(HttpClient httpClient)
        {
            using (httpClient)
            {
                var tasks = new[]
                {
                    QueryAsync(httpClient, options, PackagePublishingStatus.Published, cancellationToken),
                    QueryAsync(httpClient, options, PackagePublishingStatus.Pending, cancellationToken),
                };

                var result = Result.Success;
                var combinedReleaseStates = ImmutableList.CreateBuilder<DpkgPackageReleaseState>();

                foreach (var task in tasks)
                {
                    var releaseStatesResult = await task.ConfigureAwait(false);

                    if (releaseStatesResult.HasValue)
                    {
                        combinedReleaseStates.AddRange(releaseStatesResult.Value);
                    }

                    result = result.Merge(releaseStatesResult);
                }

                return result.WithValue<IImmutableList<DpkgPackageReleaseState>>(combinedReleaseStates.ToImmutable());
            }
        }
    }

    private Result<HttpClient> GetHttpClient()
    {
        var result = Result.Success;

        try
        {
            var httpClient = httpClientFactory.CreateClient("Launchpad");
            return result.WithValue(httpClient);
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(new ExceptionalAnnotation(exception));
        }
    }

    private async Task<Result<ImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        HttpClient httpClient,
        DpkgReleaseStateQueryOptions options,
        PackagePublishingStatus status,
        CancellationToken cancellationToken)
    {
        var result = Result.Success;

        var ppaEndpoint = ApiEntryPoints.Production
            .With(ApiVersion.Development)
            .People(PpaOwner)
            .Ppa(PpaName);

        var ppaEndpointLocation = new Location { ResourceLocator = ppaEndpoint.EndpointRoot.AbsoluteUri };
        var archiveName = $"ppa:{PpaOwner}:{PpaName}";
        if (status != PackagePublishingStatus.Published) archiveName += $" ({status})";
        
        FragmentedCollection<PpaSourcePackagePublishingHistoryRecord> publishedSources;

        try
        {
            publishedSources = await ppaEndpoint.GetPublishedSourcesAsync(
                    httpClient: httpClient,
                    status: status,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(new ExceptionalAnnotation(exception, [ppaEndpointLocation]));
        }

        var releaseStates = ImmutableList.CreateBuilder<DpkgPackageReleaseState>();

        while (true)
        {
            foreach (var publishedSource in publishedSources.CurrentFragment.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packageNameParsingResult = DpkgName.Parse(publishedSource.SourcePackageName, ppaEndpointLocation);
                result = result.Merge(packageNameParsingResult);
                if (packageNameParsingResult.IsFailure) continue;

                var packageName = packageNameParsingResult.Value;
                if (!options.PackageNames.Contains(packageName)) continue;

                var packageVersionParsingResult = DpkgVersion.Parse(publishedSource.SourcePackageVersion, ppaEndpointLocation);
                result = result.Merge(packageVersionParsingResult);
                if (packageVersionParsingResult.IsFailure) continue;

                var componentParsingResult = DpkgComponent.Parse(publishedSource.ComponentName, ppaEndpointLocation);
                result = result.Merge(componentParsingResult);
                if (componentParsingResult.IsFailure) continue;

                var component = componentParsingResult.Value;
                if (options.Components is not [] && !options.Components.Contains(component)) continue;

                var seriesParsingResult = DpkgSeries.Parse(publishedSource.DistroSeriesLink.Name, ppaEndpointLocation);
                result = result.Merge(seriesParsingResult);
                if (seriesParsingResult.IsFailure) continue;

                var pocketParsingResult = ConvertPocket(publishedSource.Pocket, ppaEndpointLocation);
                result = result.Merge(pocketParsingResult);
                if (pocketParsingResult.IsFailure) continue;

                var suite = new DpkgSuite()
                {
                    Series = seriesParsingResult.Value,
                    Pocket = pocketParsingResult.Value,
                };

                if (options.Suites is not [] && !options.Suites.Contains(suite)) continue;

                releaseStates.Add(new DpkgPackageReleaseState(
                    Package: packageName,
                    Version: packageVersionParsingResult.Value,
                    Architecture: DpkgArchitecture.Source,
                    ArchiveSection: new DpkgArchiveSection(archiveName, component, suite),
                    IsPendingOrProposed: status != PackagePublishingStatus.Published));
            }

            if (!publishedSources.CurrentFragment.HasNextFragment) break;

            try
            {
                await publishedSources.FetchNextFragmentAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return result
                    .WithAnnotation(new ExceptionalAnnotation(exception, [ppaEndpointLocation]))
                    .WithValue(releaseStates.ToImmutable());
            }
        }

        return result.WithValue(releaseStates.ToImmutable());
    }

    private Result<DpkgPocket> ConvertPocket(
        Pocket pocket,
        Location ppaEndpointLocation)
    {
        return pocket switch
        {
            Pocket.Release => Result.Success.WithValue(UbuntuPockets.Release),
            Pocket.Updates => Result.Success.WithValue(UbuntuPockets.Updates),
            Pocket.Security => Result.Success.WithValue(UbuntuPockets.Security),
            Pocket.Proposed => Result.Success.WithValue(UbuntuPockets.Proposed),
            Pocket.Backports => Result.Success.WithValue(UbuntuPockets.Backports),
            _ => Result.Success.WithAnnotation(new ExceptionalAnnotation(
                new UnreachableException($"Unexpected value '{pocket}' as pocket. This should never happen."),
                [ppaEndpointLocation]))
        };
    }
}
