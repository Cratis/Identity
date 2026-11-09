// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.given;

/// <summary>A synthetic ES256 sender that never logs proofs, nonces or tokens.</summary>
public sealed class DPoPSender : IDisposable
{
    readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    string? _nonce;

    /// <summary>Gets the independent RFC 7638 expected thumbprint.</summary>
    public string Jkt => Base64UrlEncoder.Encode(new ECDsaSecurityKey(_key).ComputeJwkThumbprint());

    /// <summary>Signs a fresh token-endpoint proof.</summary>
    /// <param name="endpoint">The exact target.</param>
    /// <returns>The compact proof.</returns>
    public string Sign(string endpoint = IdentityHost.Issuer + "connect/token")
    {
        var point = _key.ExportParameters(false).Q;
        var header = JsonSerializer.SerializeToUtf8Bytes(new { typ = "dpop+jwt", alg = "ES256", jwk = new { kty = "EC", crv = "P-256", x = Base64UrlEncoder.Encode(point.X), y = Base64UrlEncoder.Encode(point.Y) } });
        var claims = new Dictionary<string, object> { ["jti"] = Guid.NewGuid().ToString("N"), ["htm"] = "POST", ["htu"] = endpoint, ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        if (_nonce is not null) claims["nonce"] = _nonce;
        var payload = JsonSerializer.SerializeToUtf8Bytes(claims);
        var data = Base64UrlEncoder.Encode(header) + "." + Base64UrlEncoder.Encode(payload);
        return data + "." + Base64UrlEncoder.Encode(_key.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    /// <summary>Sends a specific proof without automatic retries.</summary>
    /// <param name="browser">The browser.</param>
    /// <param name="fields">The token request fields.</param>
    /// <param name="proof">The exact proof, or null for no header.</param>
    /// <returns>The response, owned by the caller.</returns>
    public static async Task<HttpResponseMessage> Send(Browser browser, IEnumerable<KeyValuePair<string, string>> fields, string? proof)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token") { Content = new FormUrlEncodedContent(fields) };
        if (proof is not null) request.Headers.Add("DPoP", proof);
        return await browser.Client.SendAsync(request);
    }

    /// <summary>Obtains a server nonce, then retries with a new proof, never a reused jti.</summary>
    /// <param name="browser">The browser.</param>
    /// <param name="fields">The token request fields.</param>
    /// <returns>The final response.</returns>
    public async Task<HttpResponseMessage> Prove(Browser browser, KeyValuePair<string, string>[] fields)
    {
        var response = await Send(browser, fields, Sign());
        if (response.StatusCode == HttpStatusCode.BadRequest && response.Headers.TryGetValues("DPoP-Nonce", out var nonces))
        {
            _nonce = nonces.Single();
            response.Dispose();
            response = await Send(browser, fields, Sign());
        }

        if (response.Headers.TryGetValues("DPoP-Nonce", out var next)) _nonce = next.Single();
        return response;
    }

    /// <summary>Creates a code redemption request.</summary>
    /// <param name="code">The code.</param>
    /// <param name="verifier">The verifier.</param>
    /// <returns>The form fields.</returns>
    public static KeyValuePair<string, string>[] Code(string code, string verifier) =>
    [
        new("grant_type", "authorization_code"), new("client_id", Browser.ClientId), new("code", code),
        new("redirect_uri", Browser.RedirectUri), new("code_verifier", verifier), new("resource", IdentityHost.DirectMcp)
    ];

    /// <summary>Creates a refresh request.</summary>
    /// <param name="token">The refresh token.</param>
    /// <returns>The form fields.</returns>
    public static KeyValuePair<string, string>[] Refresh(string token) => [new("grant_type", "refresh_token"), new("client_id", Browser.ClientId), new("refresh_token", token)];

    /// <inheritdoc/>
    public void Dispose() => _key.Dispose();
}
