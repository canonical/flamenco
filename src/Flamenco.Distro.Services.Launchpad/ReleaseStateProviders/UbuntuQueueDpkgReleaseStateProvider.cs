using System.Collections.Immutable;
using System.Diagnostics;
using Canonical.Launchpad;
using Canonical.Launchpad.Endpoints;
using Canonical.Launchpad.Endpoints.Distro;
using Canonical.Launchpad.Entities;
using Flamenco.Distro.ReleaseInfo;
using Flamenco.Distro.Services.Abstractions;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.Services.Launchpad.ReleaseStateProviders;

public class UbuntuQueueReleaseStateProvider(IHttpClientFactory httpClientFactory) : IDpkgReleaseStateProvider
{
    public async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        DpkgReleaseStateQueryOptions options, 
        CancellationToken cancellationToken)
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
            var today = DateOnly.FromDateTime(DateTime.Today);
            var supportedSeries = 
                UbuntuReleases.All
                    .Where(r => today < r.EndOfLife && r.Released.Year >= 2022)
                    .Select(r => r.Series);
            
            using (httpClient)
            {   
                var tasks = new List<Task<Result<ImmutableList<DpkgPackageReleaseState>>>>();
        
                foreach (var series in supportedSeries)
                {
                    var distroSeries = ApiEntryPoints.Production
                        .With(ApiVersion.Development)
                        .Distribution("ubuntu")
                        .Series(series.Identifier);

                    tasks.Add(QueryAsync(
                        httpClient, 
                        options, 
                        distroSeries, 
                        PackageUploadStatus.New, 
                        cancellationToken));
                    
                    tasks.Add(QueryAsync(
                        httpClient, 
                        options, 
                        distroSeries, 
                        PackageUploadStatus.Unapproved, 
                        cancellationToken));
                }
                
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
        DistroSeriesEndpoint distroSeries, 
        PackageUploadStatus status, 
        CancellationToken cancellationToken)
    {
        var result = Result.Success;
        
        var distroSeriesEndpointLocation = new Location { ResourceLocator = distroSeries.EndpointRoot.AbsoluteUri };
        
        FragmentedCollection<PackageUpload> packageUploads;

        try
        {
            packageUploads = await distroSeries.GetPackageUploadsAsync(
                    httpClient: httpClient, 
                    name: "dotnet",
                    status: status,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(new ExceptionalAnnotation(exception, [distroSeriesEndpointLocation]));
        }
        
        var releaseStates = ImmutableList.CreateBuilder<DpkgPackageReleaseState>();

        while (true)
        {
            foreach (var packageUpload in packageUploads.CurrentFragment.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var packageNameParsingResult = DpkgName.Parse(packageUpload.PackageName, distroSeriesEndpointLocation);
                result = result.Merge(packageNameParsingResult);
                if (packageNameParsingResult.IsFailure) continue;
                
                var packageName = packageNameParsingResult.Value;
                if (!options.PackageNames.Contains(packageName)) continue;
                
                var packageVersionParsingResult = DpkgVersion.Parse(packageUpload.PackageVersion, distroSeriesEndpointLocation);
                result = result.Merge(packageVersionParsingResult);
                if (packageVersionParsingResult.IsFailure) continue;

                var componentParsingResult = DpkgComponent.Parse(packageUpload.ComponentName, distroSeriesEndpointLocation);
                result = result.Merge(componentParsingResult);
                if (componentParsingResult.IsFailure) continue;

                var component = componentParsingResult.Value;
                if (options.Components is not [] && !options.Components.Contains(component)) continue;

                var seriesParsingResult = DpkgSeries.Parse(packageUpload.DistroSeriesLink.Name, distroSeriesEndpointLocation);
                result = result.Merge(seriesParsingResult);
                if (seriesParsingResult.IsFailure) continue;

                var pocketParsingResult = ConvertPocket(packageUpload.Pocket, distroSeriesEndpointLocation);
                result = result.Merge(pocketParsingResult);
                if (pocketParsingResult.IsFailure) continue;

                var suite = new DpkgSuite()
                {
                    Series = seriesParsingResult.Value,
                    Pocket = pocketParsingResult.Value,
                };

                if (options.Suites is not [] && !options.Suites.Contains(suite)) continue;

                var archiveName = $"{FirstCharToUpper(distroSeries.Distribution.Name)} [{packageUpload.Status}]";
                
                releaseStates.Add(new DpkgPackageReleaseState(
                    Package: packageName,
                    Version: packageVersionParsingResult.Value,
                    Architecture: DpkgArchitecture.Source,
                    ArchiveSection: new DpkgArchiveSection(archiveName, component, suite),
                    IsPendingOrProposed: true));
            }
            
            if (!packageUploads.CurrentFragment.HasNextFragment) break;
            
            try
            {
                await packageUploads.FetchNextFragmentAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return result
                    .WithAnnotation(new ExceptionalAnnotation(exception, [distroSeriesEndpointLocation]))
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

    private string FirstCharToUpper(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        
        Span<char> firstLetterAsUpper = stackalloc char[1];
        firstLetterAsUpper[0] = char.ToUpper(value[0]) ;
        return string.Concat(firstLetterAsUpper, value.AsSpan(start: 1));
    }
}
