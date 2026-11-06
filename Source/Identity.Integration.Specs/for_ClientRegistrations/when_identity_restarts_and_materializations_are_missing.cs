// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_ClientRegistrations.given;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Integration.for_ClientRegistrations;

[Collection(ClientRegistrationCollection.Name)]
public class when_identity_restarts_and_materializations_are_missing : IAsyncLifetime
{
    readonly ClientRegistrationFixture _fixture = new();
    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    async Task should_rebuild_the_same_ids_without_appending_facts()
    {
        var before = await _fixture.Applications.Find(FilterDefinition<OpenIddictMongoDbApplication>.Empty).ToListAsync(TestContext.Current.CancellationToken);
        var count = (await _fixture.ClientFacts()).Count;
        await _fixture.Applications.DeleteManyAsync(FilterDefinition<OpenIddictMongoDbApplication>.Empty, TestContext.Current.CancellationToken);
        await _fixture.Restart();
        var after = await _fixture.Applications.Find(FilterDefinition<OpenIddictMongoDbApplication>.Empty).ToListAsync(TestContext.Current.CancellationToken);
        after.Select(application => application.Id).Order().ShouldEqual(before.Select(application => application.Id).Order());
        (await _fixture.ClientFacts()).Count.ShouldEqual(count);
        var browser = _fixture.Host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (status, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        status.ShouldEqual(HttpStatusCode.OK);
        var introspection = await browser.PostForm("/connect/introspect", [new("token", tokens.GetProperty("access_token").GetString()!)], (IdentityHost.IntrospectionClientId, ClientRegistrationFixture.InitialSecret));
        introspection.Body.GetProperty("active").GetBoolean().ShouldBeTrue();
    }
}
