// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.given;

/// <summary>Verifies RP-facing tokens using only discovery keys and the configured issuer.</summary>
public static class OidcTokens
{
    public static JsonElement Payload(string token) => JsonElement.Parse(Base64UrlEncoder.Decode(new JsonWebToken(token).EncodedPayload));

    public static async Task<TokenValidationResult> Validate(HttpClient client, string token, string clientId = SpecClientDescriptors.OidcWeb)
    {
        var keys = new JsonWebKeySet(await client.GetStringAsync("/.well-known/jwks")).GetSigningKeys();
        return await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = IdentityHost.Issuer,
            ValidAudience = clientId,
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidTypes = ["JWT"],
            ClockSkew = TimeSpan.FromSeconds(30)
        });
    }

    public static string AccessTokenHash(string token)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        return Base64UrlEncoder.Encode(hash[..(hash.Length / 2)]);
    }
}
