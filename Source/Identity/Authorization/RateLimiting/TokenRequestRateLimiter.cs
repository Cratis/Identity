// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Authorization.RateLimiting;

/// <summary>Bounds token admission with atomic, process-local fixed-window counters.</summary>
/// <param name="time">The window clock.</param>
/// <param name="options">The Identity options.</param>
/// <param name="logger">The value-safe refusal logger.</param>
internal sealed partial class TokenRequestRateLimiter(TimeProvider time, IOptions<IdentityOptions> options, ILogger<TokenRequestRateLimiter> logger)
{
    readonly object _gate = new();
    readonly Dictionary<(Partition Kind, string Key), Counter> _counters = [];
    readonly Counter[] _overflow = [new(), new(), new()];
    readonly RefusalLog[] _logs = [new(), new(), new()];
    DateTimeOffset _nextSweep = DateTimeOffset.MaxValue;

    enum Partition
    {
        Source = 0,
        Client = 1,
        Exchange = 2
    }

    /// <summary>Gets the number of retained non-overflow partitions.</summary>
    internal int TrackedCount
    {
        get { lock (_gate) return _counters.Count; }
    }

    /// <summary>Atomically spends all applicable budgets, counting refused attempts too.</summary>
    /// <param name="clientId">The bounded claimed client, or null.</param>
    /// <param name="grantType">The bounded requested grant, or null.</param>
    /// <param name="source">The normalized source address.</param>
    /// <returns>The admission decision with a partition-independent retry delay.</returns>
    internal Admission Acquire(string? clientId, string? grantType, string source)
    {
        var settings = options.Value.TokenRateLimit;
        if (!settings.Enabled) return new(true, 0);

        lock (_gate)
        {
            var now = time.GetUtcNow();
            Sweep(now);
            var sourceAllowed = Spend(Partition.Source, source, settings.PerSourceRequests, now, settings);
            var clientAllowed = true;
            var exchangeAllowed = true;
            if (clientId is not null)
            {
                clientAllowed = Spend(Partition.Client, clientId, settings.PerClientRequests, now, settings);
                if (grantType == TokenExchangeConstants.GrantType)
                    exchangeAllowed = Spend(Partition.Exchange, clientId, settings.PerClientExchangeRequests, now, settings);
            }

            if (sourceAllowed && clientAllowed && exchangeAllowed) return new(true, 0);
            var reason = !sourceAllowed ? Partition.Source : !clientAllowed ? Partition.Client : Partition.Exchange;
            LogRefusal(reason, clientId, now, settings.Window);
            return new(false, Math.Max(1, (int)Math.Ceiling(settings.Window.TotalSeconds)));
        }
    }

    [LoggerMessage(LogLevel.Warning, "Token endpoint rate limited: {Reason}; client {Client}; suppressed {Suppressed}")]
    private static partial void RateLimited(ILogger logger, string reason, string client, long suppressed);

    bool Spend(Partition kind, string key, int limit, DateTimeOffset now, TokenRateLimitOptions settings)
    {
        var partition = (kind, key);
        if (!_counters.TryGetValue(partition, out var counter))
        {
            counter = _overflow[(int)kind];
            if (_counters.Count < settings.MaxTrackedKeys)
            {
                counter = new();
                _counters.Add(partition, counter);
                _nextSweep = now + settings.Window < _nextSweep ? now + settings.Window : _nextSweep;
            }
        }

        if (counter.Expires <= now)
        {
            counter.Count = 0;
            counter.Expires = now + settings.Window;
        }

        // Saturate rather than wrapping an attacker-driven counter.
        if (counter.Count < long.MaxValue) counter.Count++;
        return counter.Count <= limit;
    }

    void Sweep(DateTimeOffset now)
    {
        if (now < _nextSweep) return;
        _nextSweep = DateTimeOffset.MaxValue;
        foreach (var entry in _counters)
        {
            if (entry.Value.Expires <= now)
                _counters.Remove(entry.Key);
            else if (entry.Value.Expires < _nextSweep)
                _nextSweep = entry.Value.Expires;
        }
    }

    void LogRefusal(Partition reason, string? clientId, DateTimeOffset now, TimeSpan window)
    {
        var state = _logs[(int)reason];
        if (now < state.Expires)
        {
            if (state.Suppressed < long.MaxValue) state.Suppressed++;
            return;
        }

        // Only bounded, tracked clients get a truncated hash, never their claimed value.
        var client = clientId is not null && _counters.ContainsKey((Partition.Client, clientId))
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientId)))[..8]
            : "unknown";
        RateLimited(logger, reason.ToString(), client, state.Suppressed);
        state.Suppressed = 0;
        state.Expires = now + window;
    }

    /// <summary>The admission decision.</summary>
    /// <param name="Allowed">Whether to continue the pipeline.</param>
    /// <param name="RetryAfter">The full-window retry delay, in seconds.</param>
    internal readonly record struct Admission(bool Allowed, int RetryAfter);

    sealed class Counter
    {
        internal long Count;
        internal DateTimeOffset Expires;
    }

    sealed class RefusalLog
    {
        internal long Suppressed;
        internal DateTimeOffset Expires;
    }
}
