// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Introspection;

[Collection(IdentityCollection.Name)]
public class when_a_client_introspects_an_access_token(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _confidential;
    (HttpStatusCode Status, JsonElement Body) _public;
    (HttpStatusCode Status, JsonElement Body) _anonymous;
    (HttpStatusCode Status, JsonElement Body) _wrongSecret;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var accessToken = tokens.GetProperty("access_token").GetString()!;
        _confidential = await browser.Introspect(accessToken);
        _public = await browser.PostForm("/connect/introspect", [new("client_id", Browser.ClientId), new("token", accessToken)]);
        _anonymous = await browser.PostForm("/connect/introspect", [new("token", accessToken)]);
        _wrongSecret = await browser.PostForm("/connect/introspect", [new("token", accessToken)], (IdentityHost.IntrospectionClientId, "incorrect-secret"));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_let_the_authenticated_resource_server_introspect() => _confidential.Body.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_let_a_public_client_introspect() => (_public.Status != HttpStatusCode.OK).ShouldBeTrue();
    [Fact] void should_not_let_an_anonymous_client_introspect() => (_anonymous.Status != HttpStatusCode.OK).ShouldBeTrue();
    [Fact] void should_not_let_a_client_with_an_invalid_secret_introspect() => (_wrongSecret.Status != HttpStatusCode.OK).ShouldBeTrue();
}
