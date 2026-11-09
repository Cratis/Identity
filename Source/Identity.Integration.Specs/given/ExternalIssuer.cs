// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A fake external issuer: it publishes its signing key as a JWKS document, through the HTTP handler the host uses to fetch
/// issuer keys, and mints subject tokens for the identities the upstream login has already linked.
/// </summary>
/// <remarks>
/// The issuer id is the GitHub issuer and the subject is the default GitHub user, because those are the upstream links the
/// host's fake sign-in creates; no other link needs seeding.
/// </remarks>
public sealed class ExternalIssuer : HttpMessageHandler
{
    public const string KeyId = "exchange-spec-1";
    public const string Audience = "https://identity.example/exchange";
    public const string ClientId = "authproxy";
    public const string ClientSecret = "exchange-client-secret-for-specs-0123456789";

    readonly RSA _rsa = RSA.Create(2048);

    /// <summary>
    /// Gets the issuer id the host is configured to trust.
    /// </summary>
    public string Issuer => "https://github.com";

    /// <summary>
    /// Creates a signed subject token for the default upstream user.
    /// </summary>
    /// <param name="jti">The token id; a new one when omitted.</param>
    /// <returns>The compact JWT.</returns>
    public string SubjectToken(string? jti = null)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["sub"] = FakeUpstream.DefaultUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["iat"] = now.AddSeconds(-5).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
            ["jti"] = jti ?? Guid.NewGuid().ToString()
        };
        var credentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256);
        return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(payload), credentials);
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var parameters = _rsa.ExportParameters(false);
        var jwks = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new Dictionary<string, string>
                {
                    ["kty"] = "RSA",
                    ["use"] = "sig",
                    ["kid"] = KeyId,
                    ["alg"] = "RS256",
                    ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
                    ["e"] = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        });
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jwks, System.Text.Encoding.UTF8, "application/json") });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _rsa.Dispose();
        }
    }
}
