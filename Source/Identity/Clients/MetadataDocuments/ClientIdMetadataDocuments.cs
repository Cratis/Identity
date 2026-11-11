// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Coordinates positive caching, bounded admission and one shared fetch per exact client ID.</summary>
/// <param name="fetcher">The guarded fetcher.</param>
/// <param name="options">Issuer-owned policy.</param>
/// <param name="resources">The server-owned permission catalogue.</param>
/// <param name="time">The cache and admission clock.</param>
/// <param name="logger">The reason-only logger.</param>
internal sealed class ClientIdMetadataDocuments(IMetadataDocumentFetcher fetcher, ClientIdMetadataOptions options, IResourceCatalog resources, TimeProvider time, ILogger<ClientIdMetadataDocuments> logger) : IClientIdMetadataDocuments
{
    readonly MetadataDocumentCache _cache = new(options, time);
    readonly MetadataFetchAdmission _admission = new(options, time);

    /// <inheritdoc/>
    public string? ClientIdFor(string id) => options.Enabled ? _cache.ClientIdFor(id) : null;

    /// <inheritdoc/>
    public async ValueTask<MetadataDocumentResult> Resolve(ClientId clientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Enabled || !ClientIdUrlPolicy.IsValid(clientId.Value)) return MetadataDocumentResult.Refused(MetadataRefusal.InvalidUrl);
        var host = new Uri(clientId.Value).IdnHost;
        if (!AllowsHost(host)) return MetadataDocumentResult.Refused(MetadataRefusal.InvalidUrl);
        var started = time.GetTimestamp();
        var entry = _cache.Reserve(clientId.Value, out var owner, out var hit);
        if (entry is null) return MetadataDocumentResult.Refused(MetadataRefusal.Capacity);
        if (owner) _ = Load(entry, clientId, host);
        var result = await entry.Completion.Task.WaitAsync(cancellationToken);
        // An in-flight response can be shared only if its eventual cache policy permits reuse.
        if (RequiresOwnFetch(owner, result)) return await Resolve(clientId, cancellationToken);
        logger.Lookup(host, result.Refusal, (long)time.GetElapsedTime(started).TotalMilliseconds, hit);
        return result.Descriptor is null ? result : result with { Descriptor = MetadataDescriptor.Copy(result.Descriptor) };
    }

    /// <summary>Provides a detached descriptor to the application store.</summary>
    /// <param name="clientId">The exact client identifier.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>A descriptor, or null for every refusal.</returns>
    public async ValueTask<OpenIddictApplicationDescriptor?> FindAsync(ClientId clientId, CancellationToken cancellationToken) => (await Resolve(clientId, cancellationToken)).Descriptor;

    bool AllowsHost(string host) => options.AllowedHosts.Count == 0 || options.AllowedHosts.Contains(host, StringComparer.Ordinal);

    static bool RequiresOwnFetch(bool owner, MetadataDocumentResult result) => !owner && result.Descriptor is not null && result.Lifetime <= TimeSpan.Zero;

    async Task Load(MetadataDocumentCacheEntry entry, ClientId clientId, string host)
    {
        if (!_admission.TryEnter(host))
        {
            _cache.Complete(entry, MetadataDocumentResult.Refused(MetadataRefusal.RateLimited));
            return;
        }

        try
        {
            // Waiter cancellation must not cancel another caller's shared fetch. The shared operation has its own deadline.
            using var deadline = new CancellationTokenSource(options.RequestTimeout, time);
            var result = await fetcher.FetchAsync(clientId, deadline.Token);
            if (result.Descriptor is not null)
            {
                result = result with { Descriptor = MetadataDescriptor.Copy(result.Descriptor) };
                MetadataDescriptor.ApplyResources(result.Descriptor, resources);
            }

            _cache.Complete(entry, result);
        }
        catch (OperationCanceledException)
        {
            _cache.Complete(entry, MetadataDocumentResult.Refused(MetadataRefusal.Timeout));
        }
        catch (Exception exception)
        {
            _cache.Fail(entry, exception);
        }
        finally
        {
            _admission.Exit();
        }
    }
}
