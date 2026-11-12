// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Integration.for_ClientIdMetadataDocuments.given;
using Cratis.Identity.Protocol.Stores;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Cratis.Identity.Integration.for_ClientIdMetadataDocuments;

[Collection(MetadataIdentityCollection.Name)]
[Requirement(Requirements.CimdDiscovery)]
[Requirement(Requirements.CimdConsent)]
[Requirement(Requirements.CimdRestartRefresh)]
[Requirement(Requirements.CimdRevalidation)]
[Requirement(Requirements.CimdPublicProfile)]
public class when_using_public_metadata_clients(MetadataIdentityHost host)
{
    [Fact]
    public async Task should_advertise_cimd_and_none_on_the_resource_lane_without_dynamic_registration()
    {
        var browser = host.CreateBrowser();
        using var json = JsonDocument.Parse(await browser.Client.GetStringAsync("/.well-known/oauth-authorization-server", TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("client_id_metadata_document_supported").GetBoolean().ShouldBeTrue();
        json.RootElement.GetProperty("token_endpoint_auth_methods_supported").EnumerateArray().Select(value => value.GetString()).ShouldContain("none");
        json.RootElement.TryGetProperty("registration_endpoint", out _).ShouldBeFalse();
        json.RootElement.TryGetProperty("client_registration_types_supported", out _).ShouldBeFalse();
        using var identity = JsonDocument.Parse(await browser.Client.GetStringAsync("/.well-known/openid-configuration", TestContext.Current.CancellationToken));
        identity.RootElement.EnumerateObject().Select(property => property.Name).ShouldContainOnly("issuer", "jwks_uri");
    }

    [Theory]
    [InlineData("native", "http://127.0.0.1/callback", "http://127.0.0.1:49152/callback")]
    [InlineData("web", "https://client.example/callback", "https://client.example/callback")]
    public async Task should_authorize_issue_rotate_and_refresh_with_a_fresh_provider(string kind, string declaration, string redirectUri)
    {
        var id = "https://client.example/" + kind + ".json";
        host.SetDocument(id, Document(id, declaration));
        var browser = host.CreateBrowser();
        var pkce = Browser.Pkce();
        using var page = await browser.Authorize(Parameters(id, redirectUri, pkce.Challenge));
        page.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.ShouldContain("Client identity: client.example");
        html.ShouldContain("Name provided by the client (not verified):");
        html.ShouldNotContain("<script>");
        html.ShouldNotContain("\u202e", StringComparison.Ordinal);
        if (kind == "native") html.ShouldContain("Any local program can use this loopback client identity.");
        using var redirected = await browser.Consent(html);
        var code = QueryHelpers.ParseQuery(redirected.Headers.Location.Query)["code"].ToString();
        var token = await browser.PostForm("/connect/token",
        [
            new("grant_type", "authorization_code"), new("client_id", id), new("code", code),
            new("redirect_uri", redirectUri), new("code_verifier", pkce.Verifier), new("resource", IdentityHost.DirectMcp)
        ]);
        token.Status.ShouldEqual(HttpStatusCode.OK);
        new JsonWebToken(token.Body.GetProperty("access_token").GetString()).Audiences.ShouldContainOnly(IdentityHost.DirectMcpAudience);
        var refresh = token.Body.GetProperty("refresh_token").GetString();

        // A new host/provider retains the real database, keys, fences and grants but has no metadata or reverse-ID cache.
        var credentials = host.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        await using var restarted = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<OpenIddictServerOptions>(options =>
            {
                options.SigningCredentials.Clear();
                options.SigningCredentials.AddRange(credentials.SigningCredentials);
                options.EncryptionCredentials.Clear();
                options.EncryptionCredentials.AddRange(credentials.EncryptionCredentials);
            })));
        await IdentityHost.WaitForRecovery(restarted.Services);
        await using var scope = restarted.Services.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IClientIdMetadataDocuments>();
        var applicationId = new ClientApplication(new OpenIddictApplicationDescriptor { ClientId = id }).Id;
        resolver.ClientIdFor(applicationId).ShouldBeNull();
        var freshBrowser = new Browser(restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(IdentityHost.Issuer), AllowAutoRedirect = false }));
        var rotated = await freshBrowser.PostForm("/connect/token", [new("grant_type", "refresh_token"), new("client_id", id), new("refresh_token", refresh), new("resource", IdentityHost.DirectMcp)]);
        rotated.Status.ShouldEqual(HttpStatusCode.OK);
        rotated.Body.GetProperty("refresh_token").GetString().ShouldNotEqual(refresh);
    }

    [Fact]
    public async Task should_accept_an_immediately_repaired_document_after_an_invalid_client_lookup()
    {
        const string Id = "https://client.example/repaired.json";
        var browser = host.CreateBrowser();
        var pkce = Browser.Pkce();
        host.SetDocument(Id, "{}");
        using var refused = await browser.Authorize(Parameters(Id, "http://127.0.0.1:49152/callback", pkce.Challenge));
        await AssertRejected(refused, "invalid_request");
        host.SetDocument(Id, Document(Id, "http://127.0.0.1/callback"));
        using var accepted = await browser.Authorize(Parameters(Id, "http://127.0.0.1:49152/callback", pkce.Challenge));
        accepted.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_refuse_missing_pkce_and_document_claimed_scopes_or_unknown_resources()
    {
        const string Id = "https://client.example/refusals.json";
        host.SetDocument(Id, Document(Id, "http://127.0.0.1/callback"));
        var browser = host.CreateBrowser();
        var request = Parameters(Id, "http://127.0.0.1:49152/callback", Browser.Pkce().Challenge);
        request.RemoveAll(_ => _.Key == "code_challenge" || _.Key == "code_challenge_method");
        using var missing = await browser.Authorize(request);
        await AssertRejected(missing, "invalid_request");
        var badScope = Parameters(Id, "http://127.0.0.1:49152/callback", Browser.Pkce().Challenge);
        badScope.RemoveAll(_ => _.Key == "scope");
        badScope.Add(new("scope", "root"));
        using var scope = await browser.Authorize(badScope);
        await AssertRejected(scope, "invalid_scope");
        var unknown = Parameters(Id, "http://127.0.0.1:49152/callback", Browser.Pkce().Challenge);
        unknown.RemoveAll(_ => _.Key == "resource");
        unknown.Add(new("resource", "https://unknown.example/mcp"));
        using var resource = await browser.Authorize(unknown);
        await AssertRejected(resource, "invalid_target");
    }

    [Theory]
    [InlineData("no-store", "noncacheable-store.example")]
    [InlineData("no-cache", "noncacheable-cache.example")]
    public async Task should_fetch_noncacheable_metadata_once_per_protocol_request(string cacheControl, string hostname)
    {
        var id = $"https://{hostname}/client.json";
        const string Redirect = "http://127.0.0.1:49152/callback";
        host.SetDocument(id, Document(id, "http://127.0.0.1/callback"), cacheControl);
        var browser = host.CreateBrowser();
        var pkce = Browser.Pkce();
        using var page = await browser.Authorize(Parameters(id, Redirect, pkce.Challenge));
        page.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var fetches = host.RequestsFor(id);
        (fetches is 1 or 2).ShouldBeTrue(); // Initial authorization may include the sign-in round trip.
        using var redirected = await browser.Consent(await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        redirected.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
        host.RequestsFor(id).ShouldEqual(++fetches);
        var code = QueryHelpers.ParseQuery(redirected.Headers.Location.Query)["code"].ToString();
        var token = await browser.PostForm("/connect/token",
        [
            new("grant_type", "authorization_code"), new("client_id", id), new("code", code),
            new("redirect_uri", Redirect), new("code_verifier", pkce.Verifier), new("resource", IdentityHost.DirectMcp)
        ]);
        token.Status.ShouldEqual(HttpStatusCode.OK);
        host.RequestsFor(id).ShouldEqual(++fetches);
        var rotated = await browser.PostForm("/connect/token",
        [
            new("grant_type", "refresh_token"), new("client_id", id),
            new("refresh_token", token.Body.GetProperty("refresh_token").GetString()), new("resource", IdentityHost.DirectMcp)
        ]);
        rotated.Status.ShouldEqual(HttpStatusCode.OK);
        rotated.Body.GetProperty("refresh_token").GetString().ShouldNotEqual(token.Body.GetProperty("refresh_token").GetString());
        host.RequestsFor(id).ShouldEqual(++fetches);
        host.SetDocument(id, "{}", cacheControl);
        using var refused = await browser.Authorize(Parameters(id, Redirect, Browser.Pkce().Challenge));
        await AssertRejected(refused, "invalid_request");
        host.RequestsFor(id).ShouldEqual(++fetches);
    }

    static async Task AssertRejected(HttpResponseMessage response, string error)
    {
        if (response.Headers.Location is null) response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        (await OidcErrors.From(response)).ShouldEqual(error);
    }

    static List<KeyValuePair<string, string?>> Parameters(string id, string redirect, string challenge) =>
    [
        new("client_id", id), new("response_type", "code"), new("redirect_uri", redirect),
        new("scope", "direct:mcp:read offline_access"), new("state", "synthetic-state"),
        new("code_challenge", challenge), new("code_challenge_method", "S256"), new("resource", IdentityHost.DirectMcp)
    ];

    static string Document(string id, string redirect) => JsonSerializer.Serialize(new
    {
        client_id = id,
        client_name = "<script>\u202e Synthetic Client",
        redirect_uris = new[] { redirect },
        token_endpoint_auth_method = "none",
        grant_types = new[] { "authorization_code", "refresh_token" },
        response_types = new[] { "code" },
        scope = "root",
        logo_uri = "http://169.254.169.254/"
    });
}
