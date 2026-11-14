// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Models the edge's documented introspection behavior: positive answers are cached for at most 30 seconds,
/// refusals are never cached, and any failure to get a clear <c language="text">active:true</c> answer refuses the token.
/// </summary>
public sealed class IntrospectingTokenCheck
{
    /// <summary>
    /// The longest a positive answer may be reused.
    /// </summary>
    public static readonly TimeSpan MaximumPositiveCache = TimeSpan.FromSeconds(30);

    const int MaximumEntries = 1024;
    static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5);

    readonly HttpClient _introspection;
    readonly TimeProvider _time;
    readonly TimeSpan _positiveCache;
    readonly Dictionary<string, DateTimeOffset> _entries = [];
    readonly object _lock = new();
    string _epoch;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectingTokenCheck"/> class.
    /// </summary>
    /// <param name="introspection">A client authorized to call Identity's introspection endpoint.</param>
    /// <param name="time">The clock used for cache expiry.</param>
    /// <param name="positiveCache">How long an active answer may be reused; never more than <see cref="MaximumPositiveCache"/>.</param>
    public IntrospectingTokenCheck(HttpClient introspection, TimeProvider time, TimeSpan? positiveCache = null)
    {
        var window = positiveCache ?? MaximumPositiveCache;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(window, MaximumPositiveCache);
        ArgumentOutOfRangeException.ThrowIfLessThan(window, TimeSpan.Zero);
        _introspection = introspection;
        _time = time;
        _positiveCache = window;
    }

    /// <summary>
    /// Checks whether Identity currently considers the token active.
    /// </summary>
    /// <param name="token">The bearer token, already validated locally.</param>
    /// <returns>True only for a cached or freshly confirmed active token; false for everything else.</returns>
    public async Task<bool> IsActive(string token)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var now = _time.GetUtcNow();
        if (IsCached(key, now))
        {
            return true;
        }

        try
        {
            using var timeout = new CancellationTokenSource(_requestTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/introspect")
            {
                Content = new FormUrlEncodedContent([new("token", token), new("token_type_hint", "access_token")])
            };
            using var response = await _introspection.SendAsync(request, timeout.Token);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (!IsActiveAnswer(body.RootElement))
            {
                return false;
            }

            Remember(key, now, body.RootElement);
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        {
            return false;
        }
    }

    static bool IsActiveAnswer(JsonElement body) =>
        body.ValueKind == JsonValueKind.Object &&
        body.TryGetProperty("active", out var active) &&
        active.ValueKind == JsonValueKind.True;

    bool IsCached(string key, DateTimeOffset now)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(key, out var expiry) && expiry > now;
        }
    }

    void Remember(string key, DateTimeOffset now, JsonElement body)
    {
        var expiry = now + _positiveCache;
        if (body.TryGetProperty("exp", out var exp) && exp.ValueKind == JsonValueKind.Number && exp.TryGetInt64(out var seconds))
        {
            var tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
            expiry = expiry < tokenExpiry ? expiry : tokenExpiry;
        }

        var epoch = body.TryGetProperty("cache_epoch", out var value) ? value.GetRawText() : null;
        lock (_lock)
        {
            if (epoch != _epoch)
            {
                _entries.Clear();
                _epoch = epoch;
            }

            if (_entries.Count >= MaximumEntries)
            {
                _entries.Clear();
            }

            _entries[key] = expiry;
        }
    }
}
