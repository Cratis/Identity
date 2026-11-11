// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.given;

/// <summary>Drives confidential OIDC clients through the existing browser and token fixtures.</summary>
public static class OidcRequests
{
    public const string RedirectUri = "https://rp.example/signin-oidc";
    public const string Nonce = "example-oidc-nonce";

    public static List<KeyValuePair<string, string?>> Authorization(string challenge, string clientId = SpecClientDescriptors.OidcWeb, string scope = "openid profile", string? nonce = Nonce) =>
    [
        new("client_id", clientId), new("response_type", "code"), new("redirect_uri", RedirectUri),
        new("scope", scope), new("nonce", nonce), new("state", "example-oidc-state"),
        new("code_challenge", challenge), new("code_challenge_method", "S256")
    ];

    public static async Task<string> Code(Browser browser, string challenge, string clientId = SpecClientDescriptors.OidcWeb, string scope = "openid profile")
    {
        using var page = await browser.Authorize(Authorization(challenge, clientId, scope));
        using var callback = await browser.Consent(await page.Content.ReadAsStringAsync(), tenant: null);
        if (callback.Headers.Location is not { } location) throw new InvalidOperationException("OIDC consent did not produce a callback.");
        var parameters = QueryHelpers.ParseQuery(location.Query);
        if (parameters["state"] != "example-oidc-state" || parameters["iss"] != IdentityHost.Issuer) throw new InvalidOperationException("OIDC callback did not preserve state and issuer.");
        var code = parameters["code"].ToString();
        if (string.IsNullOrEmpty(code)) throw new InvalidOperationException("OIDC consent did not produce an authorization code.");
        return code;
    }

    public static Task<(HttpStatusCode Status, JsonElement Body)> Redeem(Browser browser, string code, string verifier, string clientId = SpecClientDescriptors.OidcWeb) => browser.PostForm(
        "/connect/token",
        [new("grant_type", "authorization_code"), new("code", code), new("redirect_uri", RedirectUri), new("code_verifier", verifier)],
        (clientId, SpecClientDescriptors.OidcClientSecret));

    public static async Task<(HttpStatusCode Status, JsonElement Body)> Login(Browser browser, string clientId = SpecClientDescriptors.OidcWeb, string scope = "openid profile")
    {
        var (verifier, challenge) = Browser.Pkce();
        var code = await Code(browser, challenge, clientId, scope);
        return await Redeem(browser, code, verifier, clientId);
    }

    public static Task<(HttpStatusCode Status, JsonElement Body)> Refresh(Browser browser, string refreshToken, string clientId = SpecClientDescriptors.OidcWeb) => browser.PostForm(
        "/connect/token",
        [new("grant_type", "refresh_token"), new("refresh_token", refreshToken)],
        (clientId, SpecClientDescriptors.OidcClientSecret));
}
