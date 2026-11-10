// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_DevelopmentProfile;

[Collection(DevelopmentProfileCollection.Name)]
public class when_a_profile_client_signs_in(DevelopmentProfileHost host) : IAsyncLifetime
{
    HttpResponseMessage _authorization;
    string _consentPage;
    HttpResponseMessage _redirect;
    JsonElement _token;
    HttpStatusCode _tokenStatus;
    TokenValidationResult _local;
    TokenValidationResult _deployedIssuer;
    TokenValidationResult _restAudience;
    TokenValidationResult _unrelatedKey;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateLocalBrowser();
        var (verifier, challenge) = Browser.Pkce();
        _authorization = await browser.Authorize(
        [
            new("client_id", DevelopmentProfileHost.LocalClient),
            new("response_type", "code"),
            new("redirect_uri", DevelopmentProfileHost.LocalRedirect),
            new("scope", "direct:mcp:read"),
            new("state", "spec-state"),
            new("code_challenge", challenge),
            new("code_challenge_method", "S256"),
            new("resource", DevelopmentProfileHost.LocalDirectMcp)
        ]);
        _consentPage = await _authorization.Content.ReadAsStringAsync();
        _redirect = await browser.Consent(_consentPage, tenant: DevelopmentProfileHost.LocalTenant);
        var code = QueryHelpers.ParseQuery(_redirect.Headers.Location.Query)["code"].ToString();
        (_tokenStatus, _token) = await browser.PostForm("/connect/token",
        [
            new("grant_type", "authorization_code"),
            new("client_id", DevelopmentProfileHost.LocalClient),
            new("code", code),
            new("redirect_uri", DevelopmentProfileHost.LocalRedirect),
            new("code_verifier", verifier),
            new("resource", DevelopmentProfileHost.LocalDirectMcp)
        ]);

        var accessToken = _token.GetProperty("access_token").GetString();
        var keys = new JsonWebKeySet(await browser.Client.GetStringAsync("/.well-known/jwks")).GetSigningKeys();
        using var unrelated = RSA.Create(2048);
        _local = await Validate(accessToken, DevelopmentProfileHost.LocalIssuer, DevelopmentProfileHost.DirectMcpAudience, keys);
        _deployedIssuer = await Validate(accessToken, "https://id.example.com/", DevelopmentProfileHost.DirectMcpAudience, keys);
        _restAudience = await Validate(accessToken, DevelopmentProfileHost.LocalIssuer, DevelopmentProfileHost.DirectApiAudience, keys);
        _unrelatedKey = await Validate(accessToken, DevelopmentProfileHost.LocalIssuer, DevelopmentProfileHost.DirectMcpAudience, [new RsaSecurityKey(unrelated.ExportParameters(false))]);
    }

    public ValueTask DisposeAsync()
    {
        _authorization.Dispose();
        _redirect.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_ask_for_consent_before_issuing_a_code() => _authorization.Headers.Location.ShouldBeNull();
    [Fact] void should_show_the_consent_form() => _consentPage.ShouldContain("__consent_antiforgery");
    [Fact] void should_redirect_to_the_localhost_callback_on_the_port_the_tool_chose() => _redirect.Headers.Location.GetLeftPart(UriPartial.Path).ShouldEqual(DevelopmentProfileHost.LocalRedirect);
    [Fact] void should_issue_the_token() => _tokenStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_issue_an_id_token() => _token.TryGetProperty("id_token", out _).ShouldBeFalse();
    [Fact] void should_accept_it_against_the_local_issuer_keys_and_mcp_audience() => _local.IsValid.ShouldBeTrue();
    [Fact] void should_reject_it_for_a_deployed_issuer() => _deployedIssuer.IsValid.ShouldBeFalse();
    [Fact] void should_reject_the_issued_mcp_token_on_the_rest_audience() => _restAudience.IsValid.ShouldBeFalse();
    [Fact] void should_reject_it_with_an_unrelated_key() => _unrelatedKey.IsValid.ShouldBeFalse();

    static Task<TokenValidationResult> Validate(string token, string issuer, string audience, IEnumerable<SecurityKey> keys) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKeys = keys,
            ValidTypes = ["at+jwt"]
        });
}
