// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_TokenExchange;

[Collection(ExchangeCollection.Name)]
public class when_the_exchange_is_refused(ExchangeIdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _replayed;
    (HttpStatusCode Status, JsonElement Body) _wrongSecret;
    (HttpStatusCode Status, JsonElement Body) _unlistedTenant;
    (HttpStatusCode Status, JsonElement Body) _wrongResource;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);

        var token = host.External.SubjectToken();
        await Exchange(browser, token, IdentityHost.MemberTenant, IdentityHost.DirectMcp, ExternalIssuer.ClientSecret);
        _replayed = await Exchange(browser, token, IdentityHost.MemberTenant, IdentityHost.DirectMcp, ExternalIssuer.ClientSecret);
        _wrongSecret = await Exchange(browser, host.External.SubjectToken(), IdentityHost.MemberTenant, IdentityHost.DirectMcp, "incorrect-secret");
        _unlistedTenant = await Exchange(browser, host.External.SubjectToken(), "not-a-member-tenant", IdentityHost.DirectMcp, ExternalIssuer.ClientSecret);
        _wrongResource = await Exchange(browser, host.External.SubjectToken(), IdentityHost.MemberTenant, IdentityHost.StudioMcp, ExternalIssuer.ClientSecret);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    static Task<(HttpStatusCode Status, JsonElement Body)> Exchange(Browser browser, string subjectToken, string tenant, string resource, string secret) =>
        browser.PostForm(
            "/connect/token",
            [
                new("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
                new("subject_token", subjectToken),
                new("subject_token_type", "urn:ietf:params:oauth:token-type:jwt"),
                new("resource", resource),
                new("scope", "direct:mcp:read"),
                new("tenant", tenant)
            ],
            (ExternalIssuer.ClientId, secret));

    [Fact] void should_refuse_a_replayed_subject_token_as_invalid_grant() => _replayed.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_refuse_a_wrong_client_secret() => _wrongSecret.Body.GetProperty("error").GetString().ShouldEqual("invalid_client");
    [Fact] void should_refuse_a_tenant_the_person_is_not_a_member_of_as_invalid_grant() => _unlistedTenant.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_use_one_description_for_identity_side_refusals() => _replayed.Body.GetProperty("error_description").GetString().ShouldEqual(_unlistedTenant.Body.GetProperty("error_description").GetString());
    [Fact] void should_refuse_a_resource_the_client_is_not_allowed() => _wrongResource.Status.ShouldEqual(HttpStatusCode.BadRequest);
}
