// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// <param name="time">The <see cref="TimeProvider"/>.</param>
internal class SubjectTokenValidator(IOptions<IdentityOptions> options, IExternalIssuerKeys keys, TimeProvider time) : ISubjectTokenValidator
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

    /// <summary>
    /// Creates the single-use key of a token from its verified claims, never from its encoding: the same claims signed
    /// or encoded again map to the same key.
    /// </summary>
    /// <param name="issuer">The issuer.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="jti">The token id.</param>
    /// <param name="issuedAt">The issued-at time, used with the expiry when there is no usable id.</param>
    /// <param name="expires">The expiry time.</param>
    /// <returns>The key.</returns>
    public static string ReplayKey(string issuer, string subject, string? jti, long issuedAt, long expires)
    {
        var identity = !string.IsNullOrEmpty(jti) && jti.Length <= MaximumJtiLength ? $"jti\n{jti}" : $"iat\n{issuedAt}\n{expires}";
        var content = $"{subject.Length}:{subject}\n{identity}";
        return $"{issuer}\nsha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))}";
    }

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

        // Read the checked JSON numbers directly: JWT convenience properties can truncate fractions.
        using var payload = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parsed.EncodedPayload));
        var claims = payload.RootElement;
        var token = await Verify(rawToken, issuer, signingKeys, claims);
        return token is null ? SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid) : Interpret(token, issuer, claims);
    }

    static DateTimeOffset NumericDate(JsonElement claim) => DateTimeOffset.UnixEpoch.AddSeconds(claim.GetDouble());

    (ExternalIssuerOptions? Issuer, JsonWebToken? Token, SubjectTokenRefusal Refusal) Select(string rawToken, TokenExchangeClientOptions client)
    {
        if (string.IsNullOrEmpty(rawToken) || rawToken.Length > MaximumTokenLength || !CanonicalCompactToken.IsCanonical(rawToken))
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

    async Task<JsonWebToken?> Verify(string rawToken, ExternalIssuerOptions issuer, IReadOnlyCollection<SecurityKey> signingKeys, JsonElement claims)
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
            LifetimeValidator = (_, _, _, validation) => HasValidLifetime(claims, validation.ClockSkew)
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

    bool HasValidLifetime(JsonElement claims, TimeSpan clockSkew)
    {
        var now = time.GetUtcNow();
        if (!claims.TryGetProperty("exp", out var expires) || NumericDate(expires) <= now - clockSkew)
        {
            return false;
        }

        return !claims.TryGetProperty("nbf", out var notBefore) || NumericDate(notBefore) <= now + clockSkew;
    }

    SubjectTokenResult Interpret(JsonWebToken token, ExternalIssuerOptions issuer, JsonElement claims)
    {
        if (!claims.TryGetProperty("iat", out var issuedAt) || string.IsNullOrEmpty(token.Subject))
        {
            return SubjectTokenResult.Refused(SubjectTokenRefusal.MissingClaim);
        }

        var now = time.GetUtcNow();
        var issued = NumericDate(issuedAt);
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

        var expiry = NumericDate(claims.GetProperty("exp"));
        var replayKey = ReplayKey(issuer.Issuer, token.Subject, token.Id, issued.ToUnixTimeSeconds(), expiry.ToUnixTimeSeconds());
        long? authTime = token.TryGetPayloadValue<long>("auth_time", out var seconds) ? seconds : null;
        return SubjectTokenResult.Accepted(new ValidatedSubject(upstreamIssuer, upstreamSubject, authTime, expiry, issued, replayKey));
    }
}
