// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Integration.for_ClientRegistrations.given;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Integration.for_ClientRegistrations;

[Collection(ClientRegistrationCollection.Name)]
public class when_the_configured_secret_is_rotated : IAsyncLifetime
{
    readonly ClientRegistrationFixture _fixture = new();
    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    async Task should_append_one_rotation_and_accept_only_the_new_secret()
    {
        var before = (await _fixture.ClientFacts()).Count;
        await _fixture.Restart(ClientRegistrationFixture.RotatedSecret);
        var after = await _fixture.ClientFacts();
        after.Count.ShouldEqual(before + 1);
        var rotationId = ((Cratis.Chronicle.Events.EventTypeAttribute)Attribute.GetCustomAttribute(typeof(ClientSecretRotated), typeof(Cratis.Chronicle.Events.EventTypeAttribute))!).Id.Value;
        after.Count(document => document.ToString().Contains(rotationId, StringComparison.Ordinal)).ShouldEqual(2);
        await using var scope = _fixture.Host.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = (await manager.FindByClientIdAsync(IdentityHost.IntrospectionClientId, TestContext.Current.CancellationToken))!;
        (await manager.ValidateClientSecretAsync(application, ClientRegistrationFixture.RotatedSecret, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await manager.ValidateClientSecretAsync(application, ClientRegistrationFixture.InitialSecret, TestContext.Current.CancellationToken)).ShouldBeFalse();
        var browser = _fixture.Host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var token = tokens.GetProperty("access_token").GetString()!;
        var current = await browser.PostForm("/connect/introspect", [new("token", token)], (IdentityHost.IntrospectionClientId, ClientRegistrationFixture.RotatedSecret));
        current.Body.GetProperty("active").GetBoolean().ShouldBeTrue();
        var previous = await browser.PostForm("/connect/introspect", [new("token", token)], (IdentityHost.IntrospectionClientId, ClientRegistrationFixture.InitialSecret));
        (previous.Status != HttpStatusCode.OK).ShouldBeTrue();
    }
}
