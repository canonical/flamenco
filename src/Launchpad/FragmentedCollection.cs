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
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Canonical.Launchpad.Exceptions;

namespace Canonical.Launchpad;

/// <summary>
/// A collection that has to be fetched in fragments.
/// </summary>
/// <typeparam name="TEntry">Type of the entries in the collection.</typeparam>
public class FragmentedCollection<TEntry>
{
    private readonly HttpClient _httpClient;
    
    private FragmentedCollection(HttpClient httpClient)
    {
        _httpClient = httpClient;
        CurrentFragment = default;
    }

    internal static async Task<FragmentedCollection<TEntry>> FetchAsync(
        Uri collectionLink,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var collection = new FragmentedCollection<TEntry>(httpClient);
        await collection.FetchFragmentAsync(collectionLink, cancellationToken).ConfigureAwait(false);
        return collection;
    }
    
    /// <summary>
    /// Total count of entries in the entire collection.
    /// </summary>
    public int Count => CurrentFragment.TotalSize;

    /// <summary>
    /// Current collection fragment that is cached locally.
    /// </summary>
    public CollectionFragment<TEntry> CurrentFragment { get; private set; }

    /// <summary>
    /// Fetches the next fragment asynchronously.
    /// </summary>
    /// <remarks>
    /// Check <see cref="CollectionFragment{TEntry}.HasNextFragment"/> before you call this method to avoid
    /// unnecessary network calls and exceptions. 
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A representation of the asynchronous operation.</returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task FetchNextFragmentAsync(CancellationToken cancellationToken) =>
        FetchFragmentAsync(CurrentFragment.NextFragmentLink, cancellationToken);
    
    /// <summary>
    /// Fetches the previous fragment asynchronously.
    /// </summary>
    /// <remarks>
    /// Check <see cref="CollectionFragment{TEntry}.HasPreviousFragment"/> before you call this method to avoid
    /// unnecessary network calls and exceptions. 
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A representation of the asynchronous operation.</returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public Task FetchPreviousFragmentAsync(CancellationToken cancellationToken) =>
        FetchFragmentAsync(CurrentFragment.PreviousFragmentLink, cancellationToken);
    
    private async Task FetchFragmentAsync(Uri? fragmentUri, CancellationToken cancellationToken)
    {
        if (fragmentUri == null) throw new NotFoundException();

        var result = await _httpClient
            .GetAndParseJsonFromLaunchpadAsync<CollectionFragment<TEntry>>(fragmentUri, cancellationToken)
            .ConfigureAwait(false);
        
        CurrentFragment = result;
    }

    /// <summary>
    /// Enumerate through all entries of the collection beginning with current fragment.
    /// </summary>
    /// <param name="fetchNext">
    /// A callback function that will be invoked every time before the next fragment will be fetched. If the function
    /// returns false the method will return and no further fragments will be fetched.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>An enumerator that provides asynchronous iteration over the collection.</returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async IAsyncEnumerable<TEntry> EnumerateToEndAsync(
        Func<bool>? fetchNext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            foreach (var entry in CurrentFragment.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entry;
            }

            if (!CurrentFragment.HasNextFragment) yield break;
            if (fetchNext is not null && !fetchNext()) yield break;
            await FetchNextFragmentAsync(cancellationToken).ConfigureAwait(false);    
        }
    }
    
    /// <summary>
    /// Enumerate through all entries of the collection in reverse beginning at the end of the current fragment.
    /// </summary>
    /// <param name="fetchPrevious">
    /// A callback function that will be invoked every time before the previous fragment will be fetched. If the
    /// function returns false the method will return and no further fragments will be fetched.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used by this operation to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>An enumerator that provides asynchronous iteration over the collection in reverse.</returns>
    /// <exception cref="NotFoundException">The requested resource was not found.</exception>
    /// <exception cref="ServiceUnavailableException">The Launchpad service is currently unavailable.</exception>
    /// <exception cref="ClientError">Generic error for client side failures.</exception>
    /// <exception cref="ServerError">Generic error for server side failures.</exception>
    /// <exception cref="ParsingError">The response could not get parsed.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async IAsyncEnumerable<TEntry> EnumerateToStartAsync(
        Func<bool>? fetchPrevious = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            for (int i = CurrentFragment.Entries.Count - 1; i >= 0; --i)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return CurrentFragment.Entries[i];
            }

            if (!CurrentFragment.HasPreviousFragment) yield break;
            if (fetchPrevious is not null && !fetchPrevious()) yield break;
            await FetchPreviousFragmentAsync(cancellationToken).ConfigureAwait(false);    
        }
    }
}

/// <summary>
/// One fragment of a collection.
/// </summary>
/// <typeparam name="TEntry">Type of the entries in the collection.</typeparam>
public readonly record struct CollectionFragment<TEntry>
{
    /// <summary>
    /// Initializes an empty <see cref="CollectionFragment{TEntry}"/>.
    /// </summary>
    public CollectionFragment()
    {
        Offset = 0;
        TotalSize = 0;
        Entries = ImmutableList<TEntry>.Empty.ToList();
        PreviousFragmentLink = null;
        NextFragmentLink = null;
    }

    /// <summary>
    /// The offset of the fragment from the first element in the collection.
    /// </summary>
    [JsonRequired]
    [JsonPropertyName(name: "start")]
    public int Offset { get; init; }
    
    /// <summary>
    /// The total size of the collection.
    /// </summary>
    [JsonInclude]
    [JsonRequired]
    [JsonPropertyName(name: "total_size")]
    internal int TotalSize { get; init; }

    /// <summary>
    /// The entries in this fragment.
    /// </summary>
    [JsonRequired]
    [JsonPropertyName(name: "entries")]
    public List<TEntry> Entries { get; init; }
    
    /// <summary>
    /// The link to the previous fragment.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(name: "prev_collection_link")]
    internal Uri? PreviousFragmentLink { get; init; }

    /// <summary>
    /// Determines if there exists a preceding fragment in the collection.  
    /// </summary>
    [JsonIgnore]
    public bool HasPreviousFragment => PreviousFragmentLink is not null;
    
    /// <summary>
    /// The link to the next fragment.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(name: "next_collection_link")]
    internal Uri? NextFragmentLink { get; init; }
    
    /// <summary>
    /// Determines if there exists a succeeding fragment in the collection.
    /// </summary>
    [JsonIgnore]
    public bool HasNextFragment => NextFragmentLink is not null;
}