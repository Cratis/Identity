// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.AsMetadataFromPrm)]
[Collection(IdentityCollection.Name)]
public class when_discovering_identity_from_a_protected_resource(IdentityHost host) : Specification
{
    McpResourceStub _resource;
    HttpClient _client;
    Browser _browser;
    JsonElement _prm;
    JsonElement _metadata;
    string _issuer;
    HttpStatusCode _challengeStatus;
    string _challengeScheme;
    HttpStatusCode _tokenStatus;
    HttpStatusCode _acceptedStatus;
    HttpStatusCode _otherTokenStatus;
    HttpStatusCode _rejectedStatus;

    async Task Establish()
    {
        _resource = await McpResourceStub.Start();
        _client = new HttpClient { BaseAddress = _resource.Address };
        _browser = host.CreateBrowser();

        using var challenge = await _client.GetAsync("/mcp");
        _challengeStatus = challenge.StatusCode;
        var authenticate = challenge.Headers.WwwAuthenticate.Single();
        _challengeScheme = authenticate.Scheme;
        var metadataUri = authenticate.Parameter.Split('"')[1];
        using var prm = await _client.GetAsync(metadataUri);
        _prm = await Browser.ReadJson(prm);
        _issuer = _prm.GetProperty("authorization_servers")[0].GetString();

        using var discovery = await _browser.Client.GetAsync(new Uri(new Uri(_issuer), "/.well-known/oauth-authorization-server"));
        _metadata = await Browser.ReadJson(discovery);
        var keys = new JsonWebKeySet(await _browser.Client.GetStringAsync(_metadata.GetProperty("jwks_uri").GetString()));
        _resource.Trust(_metadata.GetProperty("issuer").GetString(), keys.GetSigningKeys());

        var (verifier, pkce) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(pkce, [_prm.GetProperty("resource").GetString()]);
        using var page = await _browser.Send(new HttpRequestMessage(HttpMethod.Get, $"{_metadata.GetProperty("authorization_endpoint").GetString()}{QueryString.Create(parameters)}"));
        using var redirect = await _browser.Consent(await page.Content.ReadAsStringAsync());
        var code = QueryHelpers.ParseQuery(redirect.Headers.Location.Query)["code"].ToString();
        using var tokens = await _browser.RedeemCodeResponse(code, verifier, resource: _prm.GetProperty("resource").GetString(), tokenEndpoint: _metadata.GetProperty("token_endpoint").GetString());
        _tokenStatus = tokens.StatusCode;
        var body = await Browser.ReadJson(tokens);
        _acceptedStatus = await CallResource(body.GetProperty("access_token").GetString());

        // A real token, signed by this issuer but minted for a different resource, must not be accepted.
        var (otherVerifier, otherChallenge) = Browser.Pkce();
        using var otherPage = await _browser.Authorize(Browser.CliAuthorization(otherChallenge, [IdentityHost.StudioMcp], "studio:read offline_access"));
        using var otherRedirect = await _browser.Consent(await otherPage.Content.ReadAsStringAsync(), tenant: "studio");
        var otherCode = QueryHelpers.ParseQuery(otherRedirect.Headers.Location.Query)["code"].ToString();
        var (status, otherTokens) = await _browser.RedeemCode(otherCode, otherVerifier, IdentityHost.StudioMcp);
        _otherTokenStatus = status;
        _rejectedStatus = await CallResource(otherTokens.GetProperty("access_token").GetString());
    }

    [Fact] void should_challenge_an_unauthenticated_resource_request() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _challengeStatus.ShouldEqual(HttpStatusCode.Unauthorized));
    [Fact] void should_advertise_bearer_discovery() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _challengeScheme.ShouldEqual("Bearer"));
    [Fact] void should_identify_the_registered_resource() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _prm.GetProperty("resource").GetString().ShouldEqual(IdentityHost.DirectMcp));
    [Fact] void should_publish_metadata_for_the_discovered_issuer() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _metadata.GetProperty("issuer").GetString().ShouldEqual(_issuer));
    [Fact] void should_issue_a_token_using_the_discovered_endpoints() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _tokenStatus.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_accept_the_signature_issuer_type_and_audience() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _acceptedStatus.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_issue_a_real_token_for_the_other_resource() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _otherTokenStatus.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_refuse_a_valid_token_for_the_wrong_audience() => Conformance.Conformance.Expect(Requirements.AsMetadataFromPrm, () => _rejectedStatus.ShouldEqual(HttpStatusCode.Unauthorized));

    async Task<HttpStatusCode> CallResource(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    async Task Destroy()
    {
        _client?.Dispose();
        _browser?.Client.Dispose();
        if (_resource is not null)
        {
            await _resource.DisposeAsync();
        }
    }
}
