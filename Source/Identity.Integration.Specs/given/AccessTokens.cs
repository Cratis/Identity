// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Validates access tokens the way a resource server does: signature against the published JWKS, issuer and audience.
/// </summary>
public static class AccessTokens
{
    /// <summary>
    /// Validates an access token for an audience.
    /// </summary>
    /// <param name="browser">The <see cref="Browser"/> used to fetch the JWKS.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="audience">The expected audience.</param>
    /// <returns>The <see cref="TokenValidationResult"/>.</returns>
    public static async Task<TokenValidationResult> Validate(Browser browser, string accessToken, string audience = "direct-mcp")
    {
        var jwks = new JsonWebKeySet(await browser.Client.GetStringAsync("/.well-known/jwks"));
        return await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = IdentityHost.Issuer,
            ValidAudience = audience,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidTypes = ["at+jwt"]
        });
    }
}
