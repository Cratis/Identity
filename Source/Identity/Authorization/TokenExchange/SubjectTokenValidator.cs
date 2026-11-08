// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Validates the subject token of an RFC 8693 exchange against the configured external issuer. Only the issuer
/// the token names, its configured keys, algorithms and audiences count; headers that point at keys
/// (<c language="text">jku</c>, <c language="text">jwk</c>, <c language="text">x5u</c>) are never followed.
/// </summary>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
/// <param name="keys">The <see cref="IExternalIssuerKeys"/>.</param>
/// <param name="replay">The <see cref="SubjectTokenReplayGuard"/>.</param>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
internal class SubjectTokenValidator(IOptions<IdentityOptions> options, IExternalIssuerKeys keys, SubjectTokenReplayGuard replay, TimeProvider time) : ISubjectTokenValidator
{
    /// <summary>
    /// The longest subject token accepted, in characters.
    /// </summary>
    internal const int MaximumTokenLength = 8 * 1024;

    const int MaximumJtiLength = 256;

    static readonly string[] _validTypes = ["JWT", "at+jwt"];

    /// <summary>Treats a validated token time as UTC.</summary>
    /// <param name="value">The time.</param>
    /// <returns>The UTC offset time.</returns>
    public static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>Creates the single-use key of a token.</summary>
    /// <param name="issuer">The issuer.</param>
    /// <param name="jti">The token id.</param>
    /// <param name="rawToken">The raw token, digested when there is no usable id.</param>
    /// <returns>The key.</returns>
    public static string ReplayKey(string issuer, string? jti, string rawToken) =>
        !string.IsNullOrEmpty(jti) && jti.Length <= MaximumJtiLength
            ? $"{issuer}\n{jti}"
            : $"{issuer}\nsha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))}";

    /// <inheritdoc/>
    public async Task<SubjectTokenResult> Validate(string rawToken, TokenExchangeClientOptions client, CancellationToken cancellationToken = default)
    {
        var (issuer, parsed, early) = Select(rawToken, client);
        if (issuer is null || parsed is null)
        {
            return SubjectTokenResult.Refused(early);
        }

        var signingKeys = await keys.Get(issuer, parsed.Kid, cancellationToken);
        if (signingKeys is null)
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.KeysUnavailable);
        }

        var token = await Verify(rawToken, issuer, signingKeys);
        return token is null ? SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid) : Interpret(token, issuer, rawToken);
    }

    (ExternalIssuerOptions? Issuer, JsonWebToken? Token, SubjectTokenRefusal Refusal) Select(string rawToken, TokenExchangeClientOptions client)
    {
        if (string.IsNullOrEmpty(rawToken) || rawToken.Length > MaximumTokenLength || rawToken.Count(_ => _ == '.') != 2)
        {
            return (null, null, SubjectTokenRefusal.Malformed);
        }

        JsonWebToken parsed;
        try
        {
            parsed = new JsonWebToken(rawToken);
        }
        catch (Exception)
        {
            return (null, null, SubjectTokenRefusal.Malformed);
        }

        // The unverified issuer only selects a configured issuer; it is never trusted or logged.
        var issuer = options.Value.TokenExchange.Issuers.SingleOrDefault(_ => string.Equals(_.Issuer, parsed.Issuer, StringComparison.Ordinal));
        if (issuer is null)
        {
            return (null, null, SubjectTokenRefusal.UnknownIssuer);
        }

        return client.Issuers.Contains(issuer.Issuer, StringComparer.Ordinal) ? (issuer, parsed, SubjectTokenRefusal.None) : (null, null, SubjectTokenRefusal.IssuerNotAllowed);
    }

    async Task<JsonWebToken?> Verify(string rawToken, ExternalIssuerOptions issuer, IReadOnlyCollection<SecurityKey> signingKeys)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer.Issuer,
            ValidateAudience = true,
            RequireAudience = true,
            ValidAudiences = [.. issuer.Audiences],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = issuer.ClockSkew,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidAlgorithms = [.. issuer.SigningAlgorithms],
            ValidTypes = _validTypes,
            LifetimeValidator = (notBefore, expires, _, validation) => expires is not null && Utc(expires.Value) + validation.ClockSkew > time.GetUtcNow() && (notBefore is null || Utc(notBefore.Value) - validation.ClockSkew <= time.GetUtcNow())
        };

        try
        {
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(rawToken, parameters);
            return result.IsValid ? result.SecurityToken as JsonWebToken : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    SubjectTokenResult Interpret(JsonWebToken token, ExternalIssuerOptions issuer, string rawToken)
    {
        if (!token.TryGetPayloadValue<long>("iat", out var issuedAt) || string.IsNullOrEmpty(token.Subject))
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.MissingClaim);
        }

        var now = time.GetUtcNow();
        var issued = DateTimeOffset.FromUnixTimeSeconds(issuedAt);
        if (issued - now > issuer.ClockSkew || now - issued > issuer.MaxTokenAge)
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.Stale);
        }

        UpstreamIssuer upstreamIssuer;
        UpstreamSubject upstreamSubject;
        try
        {
            upstreamIssuer = new UpstreamIssuer(issuer.Issuer);
            upstreamSubject = new UpstreamSubject(token.Subject);
        }
        catch (InvalidAccountValue)
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.MissingClaim);
        }

        var replayKey = ReplayKey(issuer.Issuer, token.Id, rawToken);
        if (replay.WasUsed(replayKey))
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.Replayed);
        }

        long? authTime = token.TryGetPayloadValue<long>("auth_time", out var seconds) ? seconds : null;
        var expiry = Utc(token.ValidTo);
        return SubjectTokenResult.Accepted(new ValidatedSubject(upstreamIssuer, upstreamSubject, authTime, expiry, issued, replayKey));
    }
}
