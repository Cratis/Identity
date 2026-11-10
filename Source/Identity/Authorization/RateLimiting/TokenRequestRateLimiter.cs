// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Clients;
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
    readonly Dictionary<string, SourceBudget> _sources = new(StringComparer.Ordinal);
    readonly Dictionary<(string Source, string Client), Counter> _clients = [];
    readonly Dictionary<(string Source, string Client), Counter> _exchanges = [];
    readonly SourceBudget _sourceOverflow = new();
    readonly RefusalLog[] _logs = [new(), new(), new()];
    DateTimeOffset _nextSweep = DateTimeOffset.MaxValue;

    enum Partition
    {
        Source = 0,
        Client = 1,
        Exchange = 2
    }

    /// <summary>Gets the number of retained keys, excluding fixed overflow counters.</summary>
    internal int TrackedCount
    {
        get
        {
            lock (_gate) return _sources.Count + _clients.Count + _exchanges.Count;
        }
    }

    /// <summary>Charges the source first, then source-scoped claimed-client and exchange budgets.</summary>
    /// <param name="clientId">The unverified client identifier, if safely parsed.</param>
    /// <param name="grantType">The grant type, if safely parsed.</param>
    /// <param name="source">The normalized connection source.</param>
    /// <returns>The admission decision and safe retry delay.</returns>
    internal Admission Acquire(ClientId? clientId, string? grantType, string source)
    {
        var settings = options.Value.TokenRateLimit;
        if (!settings.Enabled) return new(true, 0);

        lock (_gate)
        {
            var now = time.GetUtcNow();
            Sweep(now);
            var budget = GetSource(source, settings.MaxTrackedKeys);
            var reason = Partition.Source;
            var retry = Spend(budget.Requests, settings.PerSourceRequests, now, settings.Window);
            if (retry == TimeSpan.Zero && clientId is not null)
            {
                // A client_id is only a hint at this point. Never share its budget across sources.
                var key = (source, clientId.Value);
                reason = Partition.Client;
                retry = Spend(GetCounter(_clients, key, budget.ClientOverflow, settings.MaxTrackedKeys), settings.PerClientRequests, now, settings.Window);
                if (retry == TimeSpan.Zero && string.Equals(grantType, TokenExchangeConstants.GrantType, StringComparison.Ordinal))
                {
                    reason = Partition.Exchange;
                    retry = Spend(GetCounter(_exchanges, key, budget.ExchangeOverflow, settings.MaxTrackedKeys), settings.PerClientExchangeRequests, now, settings.Window);
                }
            }

            if (retry == TimeSpan.Zero) return new(true, 0);
            var trackedClient = clientId is not null && _clients.ContainsKey((source, clientId.Value)) ? clientId.Value : null;
            LogRefusal(reason, trackedClient, now, settings.Window);
            return new(false, Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)));
        }
    }

    static Counter GetCounter(Dictionary<(string Source, string Client), Counter> counters, (string Source, string Client) key, Counter overflow, int capacity)
    {
        if (counters.TryGetValue(key, out var counter)) return counter;
        if (counters.Count >= capacity) return overflow;
        counter = new();
        counters.Add(key, counter);
        return counter;
    }

    [LoggerMessage(LogLevel.Warning, "Token endpoint rate limited: {Reason}; client {Client}; suppressed {Suppressed}")]
    private static partial void RateLimited(ILogger logger, string reason, string client, long suppressed);

    SourceBudget GetSource(string source, int capacity)
    {
        if (_sources.TryGetValue(source, out var budget)) return budget;
        if (_sources.Count >= capacity) return _sourceOverflow;
        budget = new();
        _sources.Add(source, budget);
        return budget;
    }

    TimeSpan Spend(Counter counter, int limit, DateTimeOffset now, TimeSpan window)
    {
        if (counter.Expires <= now)
        {
            counter.Expires = now + window;
            counter.Count = 0;
        }

        if (counter.Expires < _nextSweep) _nextSweep = counter.Expires;
        if (counter.Count >= limit) return counter.Expires - now;
        counter.Count++;
        return TimeSpan.Zero;
    }

    void Sweep(DateTimeOffset now)
    {
        if (now < _nextSweep) return;
        _nextSweep = DateTimeOffset.MaxValue;
        foreach (var entry in _sources)
        {
            // A retained source owns its fallback counters. Do not discard a live fallback
            // when its (possibly older) source window expires.
            var expiry = entry.Value.Requests.Expires;
            if (entry.Value.ClientOverflow.Expires > expiry) expiry = entry.Value.ClientOverflow.Expires;
            if (entry.Value.ExchangeOverflow.Expires > expiry) expiry = entry.Value.ExchangeOverflow.Expires;
            if (expiry <= now) _sources.Remove(entry.Key);
            else if (expiry < _nextSweep) _nextSweep = expiry;
        }

        SweepCounters(_clients, now);
        SweepCounters(_exchanges, now);
    }

    void SweepCounters(Dictionary<(string Source, string Client), Counter> counters, DateTimeOffset now)
    {
        foreach (var entry in counters)
        {
            if (entry.Value.Expires <= now) counters.Remove(entry.Key);
            else if (entry.Value.Expires < _nextSweep) _nextSweep = entry.Value.Expires;
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
        var client = clientId is not null
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientId)))[..8]
            : "unknown";
        RateLimited(logger, reason.ToString(), client, state.Suppressed);
        state.Suppressed = 0;
        state.Expires = now + window;
    }

    /// <summary>The admission decision.</summary>
    /// <param name="Allowed">Whether to continue the pipeline.</param>
    /// <param name="RetryAfter">The remaining refused window, rounded up to seconds.</param>
    internal readonly record struct Admission(bool Allowed, int RetryAfter);

    sealed class SourceBudget
    {
        internal readonly Counter Requests = new();
        internal readonly Counter ClientOverflow = new();
        internal readonly Counter ExchangeOverflow = new();
    }

    sealed class Counter
    {
        internal DateTimeOffset Expires;
        internal long Count;
    }

    sealed class RefusalLog
    {
        internal long Suppressed;
        internal DateTimeOffset Expires;
    }
}
