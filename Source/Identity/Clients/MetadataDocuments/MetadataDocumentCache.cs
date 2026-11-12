// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Protocol.Stores;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>A bounded exact-key positive cache and single-flight registry. Refusals are removed immediately.</summary>
/// <param name="options">The cache limits.</param>
/// <param name="time">The expiry clock.</param>
internal sealed class MetadataDocumentCache(ClientIdMetadataOptions options, TimeProvider time)
{
    readonly object _gate = new();
    readonly Dictionary<string, MetadataDocumentCacheEntry> _entries = new(StringComparer.Ordinal);
    long _access;

    /// <summary>Gets the bounded entry count, including in-flight work.</summary>
    internal int Count
    {
        get { lock (_gate) return _entries.Count; }
    }

    /// <summary>Finds a validated ID mapping without retaining a parallel unbounded index.</summary>
    /// <param name="id">The deterministic application ID.</param>
    /// <returns>The exact client ID, or null after expiry or eviction.</returns>
    public string? ClientIdFor(string id)
    {
        lock (_gate) return _entries.Values.FirstOrDefault(_ => _.Id == id && _.Expires > time.GetUtcNow())?.Key;
    }

    /// <summary>Reserves one fetch or joins an existing flight/fresh entry.</summary>
    /// <param name="key">The exact validated URL.</param>
    /// <param name="owner">Whether this caller owns the new fetch.</param>
    /// <param name="hit">Whether this was a fresh positive cache hit.</param>
    /// <returns>The shared entry, or null at capacity.</returns>
    public MetadataDocumentCacheEntry? Reserve(string key, out bool owner, out bool hit)
    {
        lock (_gate)
        {
            owner = false;
            hit = false;
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.Access = ++_access;
                if (!entry.Completion.Task.IsCompleted || entry.Expires > time.GetUtcNow())
                {
                    hit = entry.Completion.Task.IsCompleted;
                    return entry;
                }

                _entries.Remove(key);
            }

            if (_entries.Count >= options.MaxCacheEntries && !Evict()) return null;
            owner = true;
            entry = new MetadataDocumentCacheEntry(key, ++_access);
            _entries.Add(key, entry);
            return entry;
        }
    }

    /// <summary>Publishes only validated own-key snapshots and removes every non-reusable result.</summary>
    /// <param name="entry">The owned reservation.</param>
    /// <param name="result">The fetch result.</param>
    /// <exception cref="InvalidOperationException">An application ID collision is detected.</exception>
    public void Complete(MetadataDocumentCacheEntry entry, MetadataDocumentResult result)
    {
        lock (_gate)
        {
            if (result.Descriptor is { } descriptor)
            {
                if (!string.Equals(descriptor.ClientId, entry.Key, StringComparison.Ordinal))
                {
                    result = MetadataDocumentResult.Refused(MetadataRefusal.Metadata);
                }
                else
                {
                    var snapshot = new ClientApplication(MetadataDescriptor.Copy(descriptor));
                    if (_entries.Values.Any(_ => _.Id == snapshot.Id && _.Key != entry.Key)) throw new InvalidOperationException("Metadata application identifier collision.");
                    entry.Id = snapshot.Id;
                    entry.Expires = time.GetUtcNow() + (result.Lifetime ?? options.DefaultCacheTtl);
                    result = result with { Descriptor = snapshot.Descriptor };
                }
            }

            if (result.Descriptor is null || entry.Expires <= time.GetUtcNow()) _entries.Remove(entry.Key);
            entry.Completion.SetResult(result);
        }
    }

    /// <summary>Removes a failed flight and propagates an unexpected failure to its waiters.</summary>
    /// <param name="entry">The reservation.</param>
    /// <param name="exception">The unexpected exception, never logged as remote content.</param>
    public void Fail(MetadataDocumentCacheEntry entry, Exception exception)
    {
        lock (_gate)
        {
            _entries.Remove(entry.Key);
            entry.Completion.SetException(exception);
        }
    }

    bool Evict()
    {
        var oldest = _entries.Values.Where(_ => _.Completion.Task.IsCompleted).MinBy(_ => _.Access);
        return oldest is not null && _entries.Remove(oldest.Key);
    }
}
