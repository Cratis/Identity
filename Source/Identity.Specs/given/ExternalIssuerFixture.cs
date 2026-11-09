// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.given;

/// <summary>
/// A fake external issuer: RSA and EC signing keys and the means to mint (and forge) subject tokens.
/// </summary>
public sealed class ExternalIssuerFixture : IDisposable
{
    public const string RsaKeyId = "rsa-1";
    public const string EcKeyId = "ec-1";

    readonly RSA _rsa = RSA.Create(2048);
    readonly ECDsa _ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public SigningCredentials RsaCredentials => new(new RsaSecurityKey(_rsa) { KeyId = RsaKeyId }, SecurityAlgorithms.RsaSha256);

    public SigningCredentials EcCredentials => new(new ECDsaSecurityKey(_ec) { KeyId = EcKeyId }, SecurityAlgorithms.EcdsaSha256);

    /// <summary>
    /// Gets the public keys as the issuer's JWKS document would publish them.
    /// </summary>
    public IReadOnlyCollection<SecurityKey> PublicKeys =>
    [
        new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = RsaKeyId },
        new ECDsaSecurityKey(ECDsa.Create(_ec.ExportParameters(false))) { KeyId = EcKeyId }
    ];

    /// <summary>
    /// Gets the JWKS document of the public keys.
    /// </summary>
    public string JwksDocument
    {
        get
        {
            var rsa = _rsa.ExportParameters(false);
            var ec = _ec.ExportParameters(false);
            var keys = new object[]
            {
                new Dictionary<string, string> { ["kty"] = "RSA", ["use"] = "sig", ["kid"] = RsaKeyId, ["alg"] = "RS256", ["n"] = Base64UrlEncoder.Encode(rsa.Modulus), ["e"] = Base64UrlEncoder.Encode(rsa.Exponent) },
                new Dictionary<string, string> { ["kty"] = "EC", ["use"] = "sig", ["kid"] = EcKeyId, ["crv"] = "P-256", ["x"] = Base64UrlEncoder.Encode(ec.Q.X), ["y"] = Base64UrlEncoder.Encode(ec.Q.Y) }
            };
            return JsonSerializer.Serialize(new { keys });
        }
    }

    /// <summary>
    /// Creates the default payload of a valid subject token.
    /// </summary>
    /// <param name="now">The reference time.</param>
    /// <returns>The mutable payload.</returns>
    public static Dictionary<string, object> Payload(DateTimeOffset now) => new()
    {
        ["iss"] = Configurations.ExternalIssuer,
        ["aud"] = Configurations.ExternalAudience,
        ["sub"] = "external-subject-1",
        ["iat"] = now.AddSeconds(-10).ToUnixTimeSeconds(),
        ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
        ["jti"] = Guid.NewGuid().ToString(),
        ["auth_time"] = now.AddSeconds(-30).ToUnixTimeSeconds()
    };

    /// <summary>
    /// Signs a payload.
    /// </summary>
    /// <param name="payload">The claims.</param>
    /// <param name="credentials">The signing credentials.</param>
    /// <param name="headers">Extra header values.</param>
    /// <returns>The compact JWS.</returns>
    public static string Sign(Dictionary<string, object> payload, SigningCredentials credentials, Dictionary<string, object>? headers = null) =>
        headers is null
            ? new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(payload), credentials)
            : new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(payload), credentials, headers);

    /// <summary>
    /// Forges an unsigned token with <c language="text">alg=none</c>.
    /// </summary>
    /// <param name="payload">The claims.</param>
    /// <returns>The compact token with an empty signature.</returns>
    public static string AlgNone(Dictionary<string, object> payload) =>
        $"{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "none", typ = "JWT" })))}.{Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))}.";

    /// <summary>
    /// Forges an HS256 token keyed with the public key material, the classic algorithm-confusion attack.
    /// </summary>
    /// <param name="payload">The claims.</param>
    /// <returns>The compact JWS.</returns>
    public string HmacWithPublicKey(Dictionary<string, object> payload) =>
        Sign(payload, new SigningCredentials(new SymmetricSecurityKey(_rsa.ExportRSAPublicKey()) { KeyId = RsaKeyId }, SecurityAlgorithms.HmacSha256));

    /// <summary>
    /// Signs with a key the issuer does not publish.
    /// </summary>
    /// <param name="payload">The claims.</param>
    /// <returns>The compact JWS, claiming a published <c language="text">kid</c>.</returns>
    public static string SignWithUnpublishedKey(Dictionary<string, object> payload)
    {
        using var other = RSA.Create(2048);
        return Sign(payload, new SigningCredentials(new RsaSecurityKey(other) { KeyId = RsaKeyId }, SecurityAlgorithms.RsaSha256));
    }

    public void Dispose()
    {
        _rsa.Dispose();
        _ec.Dispose();
    }
}
