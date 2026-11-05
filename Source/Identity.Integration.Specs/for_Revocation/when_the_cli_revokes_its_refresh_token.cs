// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Revocation;

[Requirement(Requirements.RevocationRevokesGrant), Requirement(Requirements.IntrospectionInactiveAfterRevoke)]
[Collection(IdentityCollection.Name)]
public class when_the_cli_revokes_its_refresh_token(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _revocationStatus;
    HttpStatusCode _refreshStatus;
    JsonElement _refresh;
    JsonElement _introspection;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);

        (_revocationStatus, _) = await browser.Revoke(tokens.GetProperty("refresh_token").GetString()!);
        (_refreshStatus, _refresh) = await browser.Refresh(tokens.GetProperty("refresh_token").GetString()!);
        (_, _introspection) = await browser.Introspect(tokens.GetProperty("access_token").GetString()!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_accept_the_revocation() => _revocationStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_to_refresh() => _refreshStatus.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_refuse_it_as_an_invalid_grant() => _refresh.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_revoke_the_access_token_of_the_grant_for_introspection() => _introspection.GetProperty("active").GetBoolean().ShouldBeFalse();
}
