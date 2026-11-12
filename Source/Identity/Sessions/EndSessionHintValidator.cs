// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Sessions;

/// <summary>Validates only realm-issued ID tokens, including recently expired logout hints.</summary>
/// <param name="server">The current realm issuer and signing ring, including overlap keys.</param>
/// <param name="applications">The registered applications.</param>
/// <param name="options">The hint age limit.</param>
/// <param name="time">The clock.</param>
internal class EndSessionHintValidator(IOptionsMonitor<OpenIddictServerOptions> server, IOpenIddictApplicationManager applications, IOptions<EndSessionOptions> options, TimeProvider time)
{
    /// <summary>The maximum accepted hint length in characters.</summary>
    internal const int MaximumHintLength = 8 * 1024;

    /// <summary>Verifies a hint without using the access-token authentication pipeline.</summary>
    /// <param name="hint">The compact JWS.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The verified hint, or null. No unverified token content is returned or logged.</returns>
    public async Task<EndSessionHint?> Validate(string hint, CancellationToken cancellationToken = default)
    {
        var token = await Verify(hint);
        if (!AcceptsToken(token))
        {
            return null;
        }

        var clientId = Audience(token);
        if (clientId is null)
        {
            return null;
        }

        var application = await FindOpenIdApplication(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        token.TryGetPayloadValue<string>("sid", out var sessionId);
        return new(clientId, token.Subject, sessionId, application);
    }

    static bool CanReadHint(string hint) => !string.IsNullOrEmpty(hint) && hint.Length <= MaximumHintLength && CanonicalCompactToken.IsCanonical(hint);

    static bool HasSupportedHeader(JsonWebToken token) => token.Typ == "JWT" && token.Alg == SecurityAlgorithms.RsaSha256 && !string.IsNullOrEmpty(token.Kid);

    static string? Audience(JsonWebToken token)
    {
        var audiences = token.Audiences.ToArray();
        if (audiences.Length != 1 || string.IsNullOrEmpty(audiences[0]))
        {
            return null;
        }

        return MatchesOptionalClaim(token, Claims.AuthorizedParty, audiences[0]) ? audiences[0] : null;
    }

    static bool HasIdentityClaims(JsonWebToken token) =>
        !string.IsNullOrWhiteSpace(token.Subject) &&
        HasNoAccessTokenClaims(token) && HasIdTokenType(token) && HasIdTokenMarker(token);

    static bool HasNoAccessTokenClaims(JsonWebToken token) =>
        !ContainsClaim(token, Claims.Scope) && !ContainsClaim(token, "cid");

    static bool HasIdTokenType(JsonWebToken token) => MatchesOptionalClaim(token, "oi_tkn_typ", "id_token");

    static bool ContainsClaim(JsonWebToken token, string name) => token.TryGetPayloadValue<object>(name, out _);

    static bool MatchesOptionalClaim(JsonWebToken token, string name, string expected) =>
        !ContainsClaim(token, name) || (token.TryGetPayloadValue<string>(name, out var value) && string.Equals(value, expected, StringComparison.Ordinal));

    static bool HasIdTokenMarker(JsonWebToken token) =>
        HasValue(token, Claims.Nonce) || HasValue(token, Claims.AccessTokenHash) || token.TryGetPayloadValue<long>(Claims.AuthenticationTime, out _);

    static bool HasValue(JsonWebToken token, string name) => token.TryGetPayloadValue<string>(name, out var value) && !string.IsNullOrEmpty(value);

    async Task<object?> FindOpenIdApplication(string clientId, CancellationToken cancellationToken)
    {
        var application = await applications.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var permissions = await applications.GetPermissionsAsync(application, cancellationToken);
        return permissions.Contains(Permissions.Prefixes.Scope + Scopes.OpenId, StringComparer.Ordinal) &&
            permissions.Contains(Permissions.Endpoints.EndSession, StringComparer.Ordinal) ? application : null;
    }

    [SuppressMessage("Security", "CA5404", Justification = "Logout hints may be expired. Validate checks the single audience against a registered OIDC client; AcceptsToken checks issuance age, expiration ordering, and not-before time after signature and issuer validation.")]
    async Task<JsonWebToken?> Verify(string hint)
    {
        if (!CanReadHint(hint))
        {
            return null;
        }

        try
        {
            var token = new JsonWebToken(hint);
            var current = server.CurrentValue;
            if (!HasSupportedHeader(token) || current.Issuer is null)
            {
                return null;
            }

            var keys = current.SigningCredentials
                .Where(_ => _.Algorithm == SecurityAlgorithms.RsaSha256 && string.Equals(_.Key.KeyId, token.Kid, StringComparison.Ordinal))
                .Select(_ => _.Key).ToArray();
            if (keys.Length == 0)
            {
                return null;
            }

            var result = await new JsonWebTokenHandler().ValidateTokenAsync(hint, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = current.Issuer.AbsoluteUri,
                ValidateAudience = false, // The single audience is checked against a registered OIDC client below.
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                TryAllIssuerSigningKeys = false,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidTypes = ["JWT"],
                ValidateLifetime = false, // Logout permits expiry; issuance age and all token times are checked below.
                ClockSkew = TimeSpan.Zero
            });
            return result.IsValid ? result.SecurityToken as JsonWebToken : null;
        }
        catch (Exception exception) when (exception is ArgumentException or SecurityTokenException)
        {
            return null;
        }
    }

    bool AcceptsToken([NotNullWhen(true)] JsonWebToken? token) => token is not null && HasIdentityClaims(token) && HasAcceptableTimes(token);

    bool HasAcceptableTimes(JsonWebToken token)
    {
        if (!token.TryGetPayloadValue<long>(Claims.IssuedAt, out var issued) || !token.TryGetPayloadValue<long>(JwtRegisteredClaimNames.Exp, out var expires))
        {
            return false;
        }

        return IsRecent(issued) && expires >= issued && HasAcceptableNotBefore(token, expires);
    }

    bool IsRecent(long issued)
    {
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        var maximum = options.Value.EndSessionHintMaxAge;
        return maximum > TimeSpan.Zero && issued > 0 && issued <= now && now - issued <= maximum.TotalSeconds;
    }

    bool HasAcceptableNotBefore(JsonWebToken token, long expires) =>
        !ContainsClaim(token, Claims.NotBefore) ||
        (token.TryGetPayloadValue<long>(Claims.NotBefore, out var notBefore) && notBefore <= time.GetUtcNow().ToUnixTimeSeconds() && notBefore <= expires);
}
