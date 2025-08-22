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
using System.Net;
using System.Text;
using Flamenco.Distro.Services.Abstractions;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Distro.Services.Madison;

public abstract class MadisonDpkgReleaseStateProvider : IDpkgReleaseStateProvider
{
    public const string HttpClientName = "Madison";
    
    private readonly IHttpClientFactory _httpClientFactory;

    protected MadisonDpkgReleaseStateProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public abstract string ArchiveName { get; }
    
    public abstract Uri Endpoint { get; }
    
    public async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        DpkgReleaseStateQueryOptions options, 
        CancellationToken cancellationToken = default)
    {
        return await FormatRequestUri(options)
            .Then(uri => QueryAsync(uri, cancellationToken))
            .ConfigureAwait(false);
    }

    protected abstract bool IsProposed(DpkgPocket pocket);
    
    protected virtual Result<Uri> FormatRequestUri(DpkgReleaseStateQueryOptions options)
    {
        var uriBuilder = new UriBuilder(Endpoint);
        var queryBuilder = new StringBuilder(uriBuilder.Query);

        if (queryBuilder.Length == 0)
            queryBuilder.Append("?text=on");
        else
            queryBuilder.Append("&text=on");

        if (options.PackageNames.Count > 0)
        {
            queryBuilder.Append("&package=");
            queryBuilder.AppendJoin(separator: ',', options.PackageNames.Select(p => p.Identifier));
        }
        
        if (options.Architectures.Count > 0)
        {
            queryBuilder.Append("&a=");
            queryBuilder.AppendJoin(separator: ',', options.Architectures.Select(a => a.Identifier));
        }
        
        if (options.Components.Count > 0)
        {
            queryBuilder.Append("&c=");
            queryBuilder.AppendJoin(separator: ',', options.Components.Select(c => c.Identifier));
        }
        
        if (options.Suites.Count > 0)
        {
            queryBuilder.Append("&s=");
            queryBuilder.AppendJoin(separator: ',', options.Suites.Select(s => s.ToString()));
        }

        queryBuilder.Append("&S=").Append(options.IncludeBinaryPackagesOfSourcePackages ? "on" : "off");
        
        uriBuilder.Query = queryBuilder.ToString();
        return uriBuilder.Uri;
    }

    private async Task<Result<IImmutableList<DpkgPackageReleaseState>>> QueryAsync(
        Uri requestUri,
        CancellationToken cancellationToken = default)
    {
        var result = Result.Success;
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return result.WithAnnotation(new UnsuccessfulMadisonRequest(
                    responseStatusCode: response.StatusCode, 
                    location: new Location { ResourceLocator = requestUri.ToString() }));
            }

            await using var responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var streamReader = new StreamReader(responseStream);

            var releaseStates = ImmutableList.CreateBuilder<DpkgPackageReleaseState>();

            for (int lineNumber = 0;; ++lineNumber)
            {
                string? line = await streamReader
                    .ReadLineAsync(cancellationToken)
                    .ConfigureAwait(false);

                var requestUriAsString = requestUri.ToString();
                if (line is null)
                {
                    return result.WithValue<IImmutableList<DpkgPackageReleaseState>>(releaseStates.ToImmutable());
                }

                var lineLocation = new Location
                {
                    ResourceLocator = requestUriAsString,
                    TextSpan = new LinePositionSpan(
                        start: new LinePosition(line: lineNumber, character: 0),
                        end: new LinePosition(line: lineNumber, character: line.Length - 1)),
                };
                
                var releaseStateResult = ParseReleaseStates(line.AsSpan(), lineLocation, releaseStates);
                result = result.Merge(releaseStateResult);
                if (result.IsFailure) return result;
            }
        }
        catch (OperationCanceledException)
        {
            return result.WithAnnotation(new OperationCanceled(
                locations: ImmutableList.Create(new Location { ResourceLocator = requestUri.ToString() })));
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(new ExceptionalAnnotation(exception, 
                locations: ImmutableList.Create(new Location { ResourceLocator = requestUri.ToString() }) ));
        }
    }

    protected Result ParseReleaseStates(
        ReadOnlySpan<char> line, 
        Location lineLocation,
        IList<DpkgPackageReleaseState> releaseStates)
    {
        // for reference, a line looks something like this:
        // " dotnet8 | 8.0.100-8.0.0~rc1-0ubuntu1     | mantic/universe | source, amd64, arm64  "

        var result = Result.Success;
        Span<(int Start, int End)> valueRanges = stackalloc (int Start, int End)[4];

        int start = 0, end, valueIndex = 0;
        for (int position = 0; position <= line.Length; ++position)
        {
            if (position == line.Length || line[position] == '|')
            {
                if (valueIndex >= valueRanges.Length)
                {
                    return result.WithAnnotation(new MalformedMadisonResponse(
                        reason: "A row of the response contains too many values.",
                        location: lineLocation));
                }
                
                end = position - 1;

                // trim spaces at end and start of value
                while (start <= end && line[start] == ' ') ++start;
                while (end >= start && line[end] == ' ') --end;
                
                if (end < start)
                {
                    return result.WithAnnotation(new MalformedMadisonResponse(
                        reason: "A value of the response is empty.",
                        location: lineLocation));
                }

                valueRanges[valueIndex++] = (start, end);
                start = position + 1;
            }
        }

        if (valueIndex != 4)
        {
            return result.WithAnnotation(new MalformedMadisonResponse(
                reason: "A row of the response contains too few values.",
                location: lineLocation));
        }

        #region parse package name
        (start, end) = valueRanges[0];
        var nameParsingResult = DpkgName.Parse(
            value: line.Slice(start, length: end - start + 1),
            location: new Location { TextSpan = new(start, end) }.Offset(lineLocation));
        result = result.Merge(nameParsingResult);
        if (result.IsFailure) return result;
        #endregion
        
        #region parse package version
        (start, end) = valueRanges[1];
        var versionParsingResult = DpkgVersion.Parse(
            value: line.Slice(start, length: end - start + 1),
            location: new Location { TextSpan = new(start, end) }.Offset(lineLocation));
        result = result.Merge(versionParsingResult);
        if (result.IsFailure) return result;
        #endregion

        #region parse archive section
        (start, end) = valueRanges[2];
        var archiveSectionValue = line.Slice(start, length: end - start + 1);
        
        int separatorIndex = archiveSectionValue.IndexOf('/');
        DpkgComponent component;
        DpkgSuite suite;
        
        if (separatorIndex < 0)
        {
            component = DpkgComponent.Main;
            
            var suiteParsingResult = DpkgSuite.Parse(
                value: archiveSectionValue, 
                location: new Location { TextSpan = new(start, end) }.Offset(lineLocation));
            result = result.Merge(suiteParsingResult);
            if (result.IsFailure) return result;
            suite = suiteParsingResult.Value;
        }
        else
        {
            if (separatorIndex + 1 == archiveSectionValue.Length)
            {
                return result.WithAnnotation(new MalformedMadisonResponse(
                    reason: $"The value '{archiveSectionValue}' does not contain an archive component value after the '/'",
                    location: lineLocation));
            }
            
            var suiteParsingResult = DpkgSuite.Parse(
                value: archiveSectionValue.Slice(start: 0, length: separatorIndex), 
                location: new Location { TextSpan = new(start, end: start + separatorIndex - 1) }
                    .Offset(lineLocation));
            result = result.Merge(suiteParsingResult);
            if (result.IsFailure) return result;
            suite = suiteParsingResult.Value;
            
            var componentParsingResult = DpkgComponent.Parse(
                value: archiveSectionValue.Slice(start: separatorIndex + 1), 
                location: new Location { TextSpan = new(start: separatorIndex + 1, end) }
                    .Offset(lineLocation));
            result = result.Merge(componentParsingResult);
            if (result.IsFailure) return result;
            component = componentParsingResult.Value;
        }
        #endregion

        #region parse architectures
        (start, _) = valueRanges[3];
        
        for (int position = start; position <= line.Length; ++position)
        {
            if (position >= line.Length || line[position] == ',')
            {
                end = position - 1;

                var architectureParsingResult = DpkgArchitecture.Parse(
                    value: line.Slice(start, length: end - start + 1),
                    location: new Location { TextSpan = new(start, end) }.Offset(lineLocation));
                result = result.Merge(architectureParsingResult);
                if (result.IsFailure) return result;
                
                releaseStates.Add(new DpkgPackageReleaseState(
                    Package: nameParsingResult.Value,
                    Version: versionParsingResult.Value,
                    Architecture: architectureParsingResult.Value,
                    ArchiveSection: new DpkgArchiveSection(ArchiveName, component, suite),
                    IsPendingOrProposed: IsProposed(suite.Pocket)));
                
                position++;
                while (position < line.Length && line[position] == ' ') ++position;
                start = position;
            }
        }
        #endregion

        return result;
    }
    
    /// <summary>
    /// Represents an error that occurs when the madison service does not return a success status code.
    /// </summary>
    public class UnsuccessfulMadisonRequest(
        HttpStatusCode responseStatusCode, 
        Location location) 
        : ErrorBase(
            identifier: "FL0031",
            title: "Unsuccessful madison request",
            message: $"Madison service returned a non-success status code '{responseStatusCode}'. ",
            locations: ImmutableList.Create(location),
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: nameof(ResponseStatusCode), value: responseStatusCode))
    {
        /// <summary>
        /// The string value of the malformed component name
        /// </summary>
        public HttpStatusCode ResponseStatusCode => FromMetadata<HttpStatusCode>(nameof(ResponseStatusCode));
    }
    
    /// <summary>
    /// Represents an error that occurs when the response to a madison service request contains an unexpected format.   
    /// </summary>
    public class MalformedMadisonResponse(
        string reason,
        Location location) 
        : ErrorBase(
            identifier: "FL0032",
            title: "Malformed madison response",
            message: $"Madison response is malformed. {reason}",
            locations: ImmutableList.Create(location))
    {
    }
}
