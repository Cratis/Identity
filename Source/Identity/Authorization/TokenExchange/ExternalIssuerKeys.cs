// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Identity.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Fetches and caches the signing keys of configured external issuers. Keys come only from the issuer's configured
/// JWKS URI: never from a URL inside a token, never through a redirect. Fails closed.
/// </summary>
/// <param name="httpClients">The <see cref="IHttpClientFactory"/> providing the restricted JWKS client.</param>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class ExternalIssuerKeys(IHttpClientFactory httpClients, TimeProvider time, ILogger<ExternalIssuerKeys> logger) : IExternalIssuerKeys
{
    /// <summary>
    /// The name of the restricted JWKS <see cref="HttpClient"/>.
    /// </summary>
    internal const string HttpClientName = "Cratis.Identity.TokenExchangeJwks";

    /// <summary>
    /// The largest JWKS document accepted, in bytes.
    /// </summary>
    internal const int MaximumDocumentBytes = 64 * 1024;

    static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(10);
    static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(60);
    static readonly TimeSpan _staleLifetime = TimeSpan.FromHours(1);

    readonly ConcurrentDictionary<string, IssuerKeys> _issuers = new(StringComparer.Ordinal);

    /// <summary>Checks whether the cache holds the named key.</summary>
    /// <param name="cached">The cached keys.</param>
    /// <param name="keyId">The key id, if any.</param>
    /// <returns>True when no key id is named or the key is present.</returns>
    public static bool HasKey(CachedKeys cached, string? keyId) => keyId is null || cached.Keys.Any(_ => string.Equals(_.KeyId, keyId, StringComparison.Ordinal));

    /// <summary>Reads the body, refusing anything over the size limit.</summary>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The text, or null when too large.</returns>
    public static async Task<string?> ReadBounded(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaximumDocumentBytes + 1];
        var read = 0;
        int count;
        while (read < buffer.Length && (count = await stream.ReadAsync(buffer.AsMemory(read), cancellationToken)) > 0)
        {
            read += count;
        }

        return read > MaximumDocumentBytes ? null : System.Text.Encoding.UTF8.GetString(buffer, 0, read);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<SecurityKey>?> Get(ExternalIssuerOptions issuer, string? keyId, CancellationToken cancellationToken = default)
    {
        var state = _issuers.GetOrAdd(issuer.Issuer, _ => new());
        if (IsFresh(state.Cached) && HasKey(state.Cached!, keyId))
        {
            return state.Cached!.Keys;
        }

        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            var cached = state.Cached;
            if (IsFresh(cached) && HasKey(cached!, keyId))
            {
                return cached!.Keys;
            }

            var now = time.GetUtcNow();
            if (state.LastAttempt is { } last && now - last < _refreshInterval)
            {
                // Single-flight and rate-limited: a forged or unknown kid cannot make us hammer the issuer.
                return IsFresh(cached) ? cached!.Keys : UsableStale(cached);
            }

            state.LastAttempt = now;
            var fetched = await Fetch(issuer, cancellationToken);
            if (fetched is null)
            {
                return IsFresh(cached) ? cached!.Keys : UsableStale(cached);
            }

            state.Cached = new CachedKeys(fetched, now);
            return fetched;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    bool IsFresh(CachedKeys? cached) => cached is not null && time.GetUtcNow() - cached.FetchedAt < _cacheLifetime;

    IReadOnlyCollection<SecurityKey>? UsableStale(CachedKeys? cached) =>
        cached is not null && time.GetUtcNow() - cached.FetchedAt <= _staleLifetime ? cached.Keys : null;

    async Task<IReadOnlyCollection<SecurityKey>?> Fetch(ExternalIssuerOptions issuer, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(issuer.JwksTimeout);
            using var client = httpClients.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, issuer.JwksUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.KeysFetchFailed(issuer.Issuer, "status");
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase) && !string.Equals(mediaType, "application/jwk-set+json", StringComparison.OrdinalIgnoreCase))
            {
                logger.KeysFetchFailed(issuer.Issuer, "content-type");
                return null;
            }

            if (response.Content.Headers.ContentLength > MaximumDocumentBytes)
            {
                logger.KeysFetchFailed(issuer.Issuer, "size");
                return null;
            }

            var document = await ReadBounded(response, timeout.Token);
            if (document is null)
            {
                logger.KeysFetchFailed(issuer.Issuer, "size");
                return null;
            }

            var keys = new JsonWebKeySet(document).GetSigningKeys();
            if (keys.Count == 0)
            {
                logger.KeysFetchFailed(issuer.Issuer, "no-keys");
                return null;
            }

            return [.. keys];
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Network failure, timeout or malformed document: fail closed, category only.
            logger.KeysFetchFailed(issuer.Issuer, "error");
            return null;
        }
    }

    /// <summary>Keys cached for one issuer.</summary>
    /// <param name="Keys">The keys.</param>
    /// <param name="FetchedAt">When fetched.</param>
    internal sealed record CachedKeys(IReadOnlyCollection<SecurityKey> Keys, DateTimeOffset FetchedAt);

    sealed class IssuerKeys
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public CachedKeys? Cached { get; set; }

        public DateTimeOffset? LastAttempt { get; set; }
    }
}
