// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Device;

/// <summary>Shared, purpose-separated rate windows. Errors always deny admission.</summary>
/// <param name="store">The shared operational store.</param>
/// <param name="options">The limits.</param>
/// <param name="time">The clock.</param>
/// <param name="logger">The outcome-only logger.</param>
internal sealed class DeviceGrantRateLimiter(IDeviceGrantStateStore store, IOptions<IdentityOptions> options, TimeProvider time, ILogger<DeviceGrantRateLimiter> logger)
{
    /// <summary>Consumes all verification budgets before any code lookup.</summary>
    /// <param name="code">The untrusted entered code.</param>
    /// <param name="account">The trusted session account.</param>
    /// <param name="ip">The host-resolved client IP.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Whether all budgets admit the request.</returns>
    public async Task<bool> Verify(string? code, string account, string? ip, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(ip)) return Refused();
        try
        {
            var limits = options.Value.DeviceGrant;
            var accountAllowed = await Attempt("account", account, limits.AttemptsPerAccount, limits.VerificationWindow, cancellationToken);
            var ipAllowed = await Attempt("ip", ip, limits.AttemptsPerIp, limits.VerificationWindow, cancellationToken);
            var codeAllowed = await Attempt("code", Normalize(code) ?? "malformed", limits.AttemptsPerCode, limits.VerificationWindow, cancellationToken);
            var globalAllowed = await Attempt("global", "verification", limits.AttemptsGlobally, limits.VerificationWindow, cancellationToken);
            return (accountAllowed && ipAllowed && codeAllowed && globalAllowed) || Refused();
        }
        catch (Exception)
        {
            return Refused();
        }
    }

    /// <summary>Normalizes a user code using the configured issuance alphabet.</summary>
    /// <param name="code">The entered user code.</param>
    /// <returns>The canonical code, or null for malformed input.</returns>
    public string? Normalize(string? code) => DeviceGrantUserCode.Normalize(code, options.Value.DeviceGrant.UserCodeLength, options.Value.DeviceGrant.UserCodeCharset);

    /// <summary>Consumes the per-IP one-minute issuance budget.</summary>
    /// <param name="ip">The host-resolved client IP.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Whether issuance is admitted.</returns>
    public async Task<bool> Issue(string? ip, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ip)) return Refused();
        try
        {
            return await Attempt("issuance-ip", ip, options.Value.DeviceGrant.IssuancesPerIp, TimeSpan.FromMinutes(1), cancellationToken) || Refused();
        }
        catch (Exception)
        {
            return Refused();
        }
    }

    /// <summary>Consumes the polling window and retains the five-second slowdown increment.</summary>
    /// <param name="code">The opaque device code.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>Null on failure, otherwise whether this request must receive slow_down.</returns>
    public async Task<bool?> Poll(string code, CancellationToken cancellationToken)
    {
        try
        {
            var id = Digest("poll", code);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var now = time.GetUtcNow().UtcDateTime;
                var previous = await store.Read(id, cancellationToken);
                var current = previous is not null && previous.ExpiresAt > now ? previous : null;
                var slow = current is not null && now < current.NextPoll;
                var interval = checked((current?.Interval ?? (int)options.Value.DeviceGrant.PollingInterval.TotalSeconds) + (slow ? 5 : 0));
                var next = new DeviceGrantState
                {
                    Id = id,
                    Revision = checked((previous?.Revision ?? 0) + 1),
                    Interval = interval,
                    NextPoll = now.AddSeconds(interval),
                    ExpiresAt = current?.ExpiresAt ?? now.Add(options.Value.DeviceGrant.CodeLifetime)
                };
                if (!await store.Replace(next, previous?.Revision, cancellationToken)) continue;
                if (slow) logger.PollSlowed();
                return slow;
            }
        }
        catch (Exception)
        {
            logger.StorageUnavailable();
            return null;
        }

        logger.StorageUnavailable();
        return null;
    }

    static string Digest(string purpose, string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{purpose}:{value}")));

    async Task<bool> Attempt(string purpose, string value, int limit, TimeSpan window, CancellationToken cancellationToken) =>
        await store.Increment(Digest(purpose, value), time.GetUtcNow().UtcDateTime, window, cancellationToken) <= limit;

    bool Refused()
    {
        logger.ThrottleRefused();
        return false;
    }
}
