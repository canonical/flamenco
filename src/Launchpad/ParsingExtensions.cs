// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Canonical.Launchpad.Endpoints;
using Canonical.Launchpad.Exceptions;

namespace Canonical.Launchpad;

internal static class ParsingExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        // see https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties?pivots=dotnet-8-0#use-a-built-in-naming-policy
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    
    public static async ValueTask<TModel> GetAndParseJsonFromLaunchpadAsync<TModel>(
        this HttpClient httpClient,
        Uri uri,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        
        try
        {
            response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new ClientError(
                message: "The request failed due to an underlying issue such as network connectivity, DNS failure, " +
                         "server certificate validation or timeout", 
                innerException: exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ClientError(message: "The requestUri is malformed.", innerException: exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound) throw new NotFoundException();
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable) throw new ServiceUnavailableException();
            
            int statusCode = (int)response.StatusCode;

            if (statusCode is >= 400 and <= 499)
            {
                throw new ClientError(message: 
                    "The server responded with a 4XX status code, which means that the request was invalid. " +
                    "This is most likely related to either malformed query values or an implementation error " +
                    $"in this library ({statusCode} {response.ReasonPhrase}).");
            }
            
            if (statusCode is >= 500 and <= 599)
            {
                throw new ServerError(message: 
                    "The server responded with a 5XX status code, which means that an error encountered an error " +
                    $"while processing the request ({statusCode} {response.ReasonPhrase}).");
            }
            
            throw new ServerError(message: 
                $"The server responded with a non 2XX (success) status code ({statusCode} {response.ReasonPhrase}).");
        }

        TModel? parsingResult;
        
        try
        {
            parsingResult = await response.Content
                .ReadFromJsonAsync<TModel>(DefaultJsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ParsingError(innerException: exception);
        }

        if (parsingResult is null) throw new ParsingError();
        return parsingResult;
    }

    public static void Split<TEndpointParent, TEndpoint>(
        this ReadOnlySpan<char> endpointRoot, 
        char separator, 
        out TEndpointParent endpointParent, 
        out ReadOnlySpan<char> endpointId) 
        where TEndpointParent : ILaunchpadEndpoint<TEndpointParent>
        where TEndpoint : ILaunchpadEndpoint<TEndpoint>
    {
        int separatorIndex = endpointRoot.LastIndexOf(separator);

        if (separatorIndex < 0)
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is not a valid {typeof(TEndpoint).Name} root uri. " +
                $"It is missing a '{separator}' segment.");
        }

        endpointParent = TEndpointParent.ParseEndpointRoot(endpointRoot[..separatorIndex]);
        endpointId = endpointRoot[(separatorIndex + 1)..];

        if (endpointId.Length == 0)
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is not a valid {typeof(TEndpoint).Name} root uri. " +
                $"It is missing an identifier segment.");
        }
    }
    
    public static void Split<TEndpointParent, TEndpoint>(
        this ReadOnlySpan<char> endpointRoot, 
        ReadOnlySpan<char> separator, 
        out TEndpointParent endpointParent, 
        out ReadOnlySpan<char> endpointId) 
        where TEndpointParent : ILaunchpadEndpoint<TEndpointParent>
        where TEndpoint : ILaunchpadEndpoint<TEndpoint>
    {
        int separatorIndex = endpointRoot.LastIndexOf(separator);

        if (separatorIndex < 0)
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is not a valid {typeof(TEndpoint).Name} root uri. " +
                $"It is missing a '{separator}' segment.");
        }

        endpointParent = TEndpointParent.ParseEndpointRoot(endpointRoot[..separatorIndex]);
        endpointId = endpointRoot[(separatorIndex + separator.Length)..];

        if (endpointId.Length == 0)
        {
            throw new FormatException(message:
                $"'{endpointRoot}' is not a valid {typeof(TEndpoint).Name} root uri. " +
                $"It is missing an identifier segment.");
        }
    }
}
