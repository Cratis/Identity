// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_ClientRegistrations.given;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Integration.for_ClientRegistrations;

[Collection(ClientRegistrationCollection.Name)]
public class when_a_registration_is_retired : IAsyncLifetime
{
    readonly ClientRegistrationFixture _fixture = new();
    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    async Task should_remove_the_document_and_refuse_authentication()
    {
        var before = (await _fixture.ClientFacts()).Count;
        await _fixture.Restart(null);
        (await _fixture.ClientFacts()).Count.ShouldEqual(before + 1);
        (await _fixture.Applications.Find(application => application.ClientId == IdentityHost.IntrospectionClientId).AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        await using var scope = _fixture.Host.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        (await manager.FindByClientIdAsync(IdentityHost.IntrospectionClientId, TestContext.Current.CancellationToken)).ShouldBeNull();
        var browser = _fixture.Host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var response = await browser.PostForm("/connect/introspect", [new("token", tokens.GetProperty("access_token").GetString()!)], (IdentityHost.IntrospectionClientId, ClientRegistrationFixture.InitialSecret));
        (response.Status != HttpStatusCode.OK).ShouldBeTrue();
    }
}
