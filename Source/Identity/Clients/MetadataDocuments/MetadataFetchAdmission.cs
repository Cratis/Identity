// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Bounded retry admission, separate from the positive metadata cache. No failures are cached.</summary>
/// <param name="options">Issuer-owned limits.</param>
/// <param name="time">The clock.</param>
internal sealed class MetadataFetchAdmission(ClientIdMetadataOptions options, TimeProvider time)
{
    readonly object _gate = new();
    readonly Dictionary<string, int> _hosts = new(StringComparer.Ordinal);
    DateTimeOffset _window = time.GetUtcNow();
    int _count;
    int _active;

    /// <summary>Admits a fetch without creating queued work or retaining client-specific failures.</summary>
    /// <param name="host">A validated canonical host.</param>
    /// <returns>Whether there is rate and concurrency capacity.</returns>
    public bool TryEnter(string host)
    {
        lock (_gate)
        {
            if (time.GetUtcNow() - _window >= TimeSpan.FromMinutes(1))
            {
                _hosts.Clear();
                _count = 0;
                _window = time.GetUtcNow();
            }

            _hosts.TryGetValue(host, out var count);
            if (_active >= options.MaxConcurrentFetches || _count >= options.MaxFetchesPerMinute || count >= options.MaxFetchesPerHostPerMinute) return false;
            if (count == 0 && _hosts.Count >= options.MaxCacheEntries) return false;
            _hosts[host] = count + 1;
            _count++;
            _active++;
            return true;
        }
    }

    /// <summary>Releases only concurrency; consumed rate capacity is not refunded.</summary>
    public void Exit()
    {
        lock (_gate) _active--;
    }
}
