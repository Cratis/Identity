// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;

namespace Cratis.Identity.Integration.for_TokenExchange;

[Collection(ExchangeCollection.Name)]
public class when_a_person_in_two_tenants_exchanges_for_each_tenant(ExchangeIdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _first;
    (HttpStatusCode Status, JsonElement Body) _second;
    JsonWebToken _firstToken;
    JsonWebToken _secondToken;

    public async ValueTask InitializeAsync()
    {
        // The upstream login creates the account and its link to the external subject.
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);

        _first = await Exchange(browser, IdentityHost.MemberTenant);
        _second = await Exchange(browser, IdentityHost.OtherMemberTenant);
        _firstToken = new JsonWebToken(_first.Body.GetProperty("access_token").GetString());
        _secondToken = new JsonWebToken(_second.Body.GetProperty("access_token").GetString());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    Task<(HttpStatusCode Status, JsonElement Body)> Exchange(Browser browser, string tenant) =>
        browser.PostForm(
            "/connect/token",
            [
                new("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
                new("subject_token", host.External.SubjectToken()),
                new("subject_token_type", "urn:ietf:params:oauth:token-type:jwt"),
                new("resource", IdentityHost.DirectMcp),
                new("scope", "direct:mcp:read"),
                new("tenant", tenant)
            ],
            (ExternalIssuer.ClientId, ExternalIssuer.ClientSecret));

    [Fact] void should_issue_for_the_first_tenant() => _first.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_for_the_second_tenant() => _second.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_bind_the_first_token_to_the_first_tenant() => _firstToken.GetClaim("tid").Value.ShouldEqual(IdentityHost.MemberTenant);
    [Fact] void should_bind_the_second_token_to_the_second_tenant() => _secondToken.GetClaim("tid").Value.ShouldEqual(IdentityHost.OtherMemberTenant);
    [Fact] void should_address_the_registered_resource() => _firstToken.Audiences.ShouldContain("direct-mcp");
    [Fact] void should_name_the_exchange_client() => _firstToken.GetClaim("azp").Value.ShouldEqual(ExternalIssuer.ClientId);
    [Fact] void should_name_the_external_issuer() => _firstToken.GetClaim("idp").Value.ShouldEqual(host.External.Issuer);
    [Fact] void should_not_issue_a_refresh_token() => _first.Body.TryGetProperty("refresh_token", out _).ShouldBeFalse();
    [Fact] void should_report_the_issued_token_type() => _first.Body.GetProperty("issued_token_type").GetString().ShouldEqual("urn:ietf:params:oauth:token-type:access_token");
    [Fact] void should_issue_a_short_lived_token() => _first.Body.GetProperty("expires_in").GetInt32().ShouldBeLessThanOrEqual(300);
}
