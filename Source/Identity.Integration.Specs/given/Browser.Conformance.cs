// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.given;

public partial class Browser
{
    /// <summary>
    /// Redeems a code without discarding the HTTP headers or parsing an error body during setup.
    /// </summary>
    /// <param name="code">The authorization code.</param>
    /// <param name="verifier">The PKCE verifier.</param>
    /// <param name="redirectUri">The redirect URI to bind to the code.</param>
    /// <param name="resource">The discovered resource indicator.</param>
    /// <param name="tokenEndpoint">The discovered token endpoint.</param>
    /// <returns>The raw response, owned by the caller.</returns>
    public async Task<HttpResponseMessage> RedeemCodeResponse(string code, string verifier, string redirectUri = RedirectUri, string resource = IdentityHost.DirectMcp, string tokenEndpoint = "/connect/token")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(
            [
                new("grant_type", "authorization_code"),
                new("client_id", ClientId),
                new("code", code),
                new("redirect_uri", redirectUri),
                new("code_verifier", verifier),
                new("resource", resource)
            ])
        };
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Reads JSON while allowing the caller to retain the response status and headers.
    /// </summary>
    /// <param name="response">The protocol response.</param>
    /// <returns>The detached response body.</returns>
    public static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
