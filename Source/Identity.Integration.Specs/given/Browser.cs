// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Drives Identity the way a browser and the CLI do: follows redirects that stay on Identity, stops at the
/// loopback redirect, fills in the consent form and calls the token endpoints.
/// </summary>
/// <param name="client">The <see cref="HttpClient"/> that keeps cookies and does not follow redirects.</param>
public partial class Browser(HttpClient client)
{
    public const string ClientId = "cratis-cli";
    public const string RedirectUri = "http://127.0.0.1:53123/callback";
    const int MaximumRedirects = 20;

    /// <summary>
    /// Gets the underlying client.
    /// </summary>
    public HttpClient Client => client;

    /// <summary>
    /// Creates a PKCE verifier and its S256 challenge.
    /// </summary>
    /// <returns>The verifier and challenge.</returns>
    public static (string Verifier, string Challenge) Pkce()
    {
        var verifier = Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlTextEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    /// <summary>
    /// Creates the parameters of a CLI authorization request.
    /// </summary>
    /// <param name="challenge">The PKCE challenge.</param>
    /// <param name="resources">The resource indicators.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The parameters.</returns>
    public static List<KeyValuePair<string, string?>> CliAuthorization(string challenge, IEnumerable<string>? resources = null, string scope = "direct:mcp:read offline_access") =>
    [
        new("client_id", ClientId),
        new("response_type", "code"),
        new("redirect_uri", RedirectUri),
        new("scope", scope),
        new("state", "spec-state"),
        new("code_challenge", challenge),
        new("code_challenge_method", "S256"),
        .. (resources ?? [IdentityHost.DirectMcp]).Select(_ => new KeyValuePair<string, string?>("resource", _))
    ];

    /// <summary>
    /// Sends a request and follows redirects that stay on Identity.
    /// </summary>
    /// <param name="request">The first request.</param>
    /// <returns>The final response: a page, an error, or a redirect leaving Identity.</returns>
    /// <exception cref="InvalidOperationException">The redirect limit was exceeded.</exception>
    public async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        var response = await client.SendAsync(request);
        var chain = new List<string> { request.RequestUri.ToString() };
        while (RedirectOnIdentity(response) is { } location)
        {
            chain.Add(location.ToString());
            response.Dispose();
            if (chain.Count > MaximumRedirects + 1)
            {
                throw new InvalidOperationException($"Identity exceeded {MaximumRedirects} redirects: {string.Join(" -> ", chain)}");
            }

            response = await client.GetAsync(location);
        }

        return response;
    }

    Uri? RedirectOnIdentity(HttpResponseMessage response)
    {
        if (response.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther))
        {
            return null;
        }

        var location = response.Headers.Location;
        return location is { IsAbsoluteUri: false } || location?.Host == client.BaseAddress.Host ? location : null;
    }

    /// <summary>
    /// Opens the authorization endpoint.
    /// </summary>
    /// <param name="parameters">The authorization request parameters.</param>
    /// <returns>The final response.</returns>
    public Task<HttpResponseMessage> Authorize(IEnumerable<KeyValuePair<string, string?>> parameters) =>
        Send(new HttpRequestMessage(HttpMethod.Get, $"/connect/authorize{Microsoft.AspNetCore.Http.QueryString.Create(parameters)}"));

    /// <summary>
    /// Submits the consent form on a consent page.
    /// </summary>
    /// <param name="consentPage">The consent page HTML.</param>
    /// <param name="decision">The decision.</param>
    /// <param name="tenant">The selected tenant.</param>
    /// <param name="includeAntiforgery">Whether to include the antiforgery token.</param>
    /// <returns>The final response.</returns>
    public Task<HttpResponseMessage> Consent(string consentPage, string decision = "allow", string? tenant = IdentityHost.MemberTenant, bool includeAntiforgery = true)
    {
        var fields = HiddenInputs.Matches(consentPage)
            .Select(_ => new KeyValuePair<string, string>(WebUtility.HtmlDecode(_.Groups["name"].Value), WebUtility.HtmlDecode(_.Groups["value"].Value)))
            .Where(_ => includeAntiforgery || _.Key != "__consent_antiforgery")
            .Append(new("consent_decision", decision))
            .ToList();

        if (tenant is not null)
        {
            fields.Add(new("consent_tenant", tenant));
        }

        return Send(new HttpRequestMessage(HttpMethod.Post, "/connect/authorize") { Content = new FormUrlEncodedContent(fields) });
    }

    /// <summary>
    /// Runs a complete CLI login and returns the authorization code.
    /// </summary>
    /// <param name="challenge">The PKCE challenge.</param>
    /// <param name="resources">The resource indicators.</param>
    /// <returns>The authorization code.</returns>
    /// <exception cref="InvalidOperationException">The consent step did not redirect.</exception>
    public async Task<string> LoginForCode(string challenge, IEnumerable<string>? resources = null)
    {
        using var page = await Authorize(CliAuthorization(challenge, resources));
        using var redirect = await Consent(await page.Content.ReadAsStringAsync());
        if (redirect.Headers.Location is null)
        {
            throw new InvalidOperationException($"The consent step did not redirect: {(int)redirect.StatusCode} {await redirect.Content.ReadAsStringAsync()}");
        }

        return QueryHelpers.ParseQuery(redirect.Headers.Location.Query)["code"].ToString();
    }

    /// <summary>
    /// Posts to a form endpoint and reads the JSON response.
    /// </summary>
    /// <param name="path">The endpoint path.</param>
    /// <param name="fields">The form fields.</param>
    /// <param name="basic">Optional client credentials for HTTP basic authentication.</param>
    /// <returns>The status code and JSON body.</returns>
    public async Task<(HttpStatusCode Status, JsonElement Body)> PostForm(string path, IEnumerable<KeyValuePair<string, string>> fields, (string Id, string Secret)? basic = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(fields) };
        if (basic is { } credentials)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(credentials.Id)}:{Uri.EscapeDataString(credentials.Secret)}")));
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonElement.Parse(text));
    }

    /// <summary>
    /// Redeems an authorization code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="verifier">The PKCE verifier.</param>
    /// <param name="resource">The resource indicator.</param>
    /// <returns>The status code and JSON body.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> RedeemCode(string code, string verifier, string resource = IdentityHost.DirectMcp) =>
        PostForm("/connect/token",
        [
            new("grant_type", "authorization_code"),
            new("client_id", ClientId),
            new("code", code),
            new("redirect_uri", RedirectUri),
            new("code_verifier", verifier),
            new("resource", resource)
        ]);

    /// <summary>
    /// Refreshes tokens.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>The status code and JSON body.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> Refresh(string refreshToken) =>
        PostForm("/connect/token",
        [
            new("grant_type", "refresh_token"),
            new("client_id", ClientId),
            new("refresh_token", refreshToken),
            new("resource", IdentityHost.DirectMcp)
        ]);

    /// <summary>
    /// Revokes a token as the CLI.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="tokenTypeHint">The token type hint.</param>
    /// <returns>The status code and JSON body.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> Revoke(string token, string tokenTypeHint = "refresh_token") =>
        PostForm("/connect/revocation", [new("client_id", ClientId), new("token", token), new("token_type_hint", tokenTypeHint)]);

    /// <summary>
    /// Introspects a token as the resource server.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The status code and JSON body.</returns>
    public Task<(HttpStatusCode Status, JsonElement Body)> Introspect(string token) =>
        PostForm("/connect/introspect", [new("token", token)], (IdentityHost.IntrospectionClientId, IdentityHost.IntrospectionClientSecret));

    [GeneratedRegex("<input type=\"hidden\" name=\"(?<name>[^\"]*)\" value=\"(?<value>[^\"]*)\">", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex HiddenInputs { get; }
}
