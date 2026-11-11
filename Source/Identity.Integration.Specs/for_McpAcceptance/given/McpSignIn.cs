// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Exercises browser consent and code redemption; no token or principal is manufactured by the acceptance specs.
/// </summary>
internal static class McpSignIn
{
    internal static async Task<JsonElement> Authorize(Browser browser, IMcpAcceptanceHarness harness, McpProduct product, (string AuthorizationEndpoint, string TokenEndpoint) endpoints)
    {
        var (verifier, challenge) = Browser.Pkce();
        var state = Guid.NewGuid().ToString("N");
        var authorization = QueryHelpers.AddQueryString(endpoints.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = harness.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = harness.RedirectUri,
            ["scope"] = $"offline_access {product.Scope}",
            ["resource"] = product.Indicator,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["tenant"] = harness.Tenant
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, authorization);
        using var page = await browser.Send(request);
        page.StatusCode.ShouldEqual(HttpStatusCode.OK);
        using var redirect = await browser.Consent(await page.Content.ReadAsStringAsync(), tenant: harness.Tenant);
        redirect.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
        var callback = redirect.Headers.Location;
        callback.GetLeftPart(UriPartial.Path).ShouldEqual(harness.RedirectUri);
        var parameters = QueryHelpers.ParseQuery(callback.Query);
        parameters["state"].ToString().ShouldEqual(state);
        parameters["iss"].ToString().ShouldEqual(harness.Issuer.AbsoluteUri);
        var code = parameters["code"].ToString();
        string.IsNullOrEmpty(code).ShouldBeFalse();
        var (status, tokens) = await browser.PostForm(endpoints.TokenEndpoint,
        [
            new("grant_type", "authorization_code"),
            new("client_id", harness.ClientId),
            new("redirect_uri", harness.RedirectUri),
            new("code", code),
            new("code_verifier", verifier),
            new("resource", product.Indicator)
        ]);
        status.ShouldEqual(HttpStatusCode.OK);
        return tokens;
    }
}
