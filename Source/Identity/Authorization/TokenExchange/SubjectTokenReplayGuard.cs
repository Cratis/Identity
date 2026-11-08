// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Refuses a second use of a subject token. The memory is bounded, expiring and local to this node: it does not
/// survive a restart and is not shared between replicas. When it is full the guard refuses, never forgets.
/// </summary>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
internal class SubjectTokenReplayGuard(TimeProvider time)
{
    /// <summary>
    /// The most tokens remembered at once.
    /// </summary>
    internal const int MaximumEntries = 10_000;

    readonly Lock _lock = new();
    readonly Dictionary<string, DateTimeOffset> _seen = new(StringComparer.Ordinal);

    /// <summary>
    /// Checks whether a token was already used.
    /// </summary>
    /// <param name="key">The <see cref="ValidatedSubject.ReplayKey"/>.</param>
    /// <returns><see langword="true"/> if the token was used before and is still remembered.</returns>
    public bool WasUsed(string key)
    {
        lock (_lock)
        {
            return _seen.TryGetValue(key, out var until) && until > time.GetUtcNow();
        }
    }

    /// <summary>
    /// Atomically checks and records a token as used.
    /// </summary>
    /// <param name="key">The <see cref="ValidatedSubject.ReplayKey"/>.</param>
    /// <param name="until">How long to remember the token: its remaining life plus the tolerated skew.</param>
    /// <returns><see langword="true"/> if the token was recorded now; <see langword="false"/> if it was used before or the guard is full.</returns>
    public bool TryRecord(string key, DateTimeOffset until)
    {
        lock (_lock)
        {
            var now = time.GetUtcNow();
            if (_seen.TryGetValue(key, out var existing) && existing > now)
            {
                return false;
            }

            if (_seen.Count >= MaximumEntries)
            {
                foreach (var expired in _seen.Where(_ => _.Value <= now).Select(_ => _.Key).ToArray())
                {
                    _seen.Remove(expired);
                }

                if (_seen.Count >= MaximumEntries)
                {
                    return false;
                }
            }

            _seen[key] = until;
            return true;
        }
    }
}
