// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Integration.for_ClientRegistrations.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_ClientRegistrations;

[Collection(ClientRegistrationCollection.Name)]
public class when_declared_clients_are_started : IAsyncLifetime
{
    readonly ClientRegistrationFixture _fixture = new();
    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    async Task should_register_usable_clients_without_persisting_the_secret()
    {
        var browser = _fixture.Host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (status, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        status.ShouldEqual(HttpStatusCode.OK);
        var token = tokens.GetProperty("access_token").GetString()!;
        var introspection = await browser.PostForm("/connect/introspect", [new("token", token)], (IdentityHost.IntrospectionClientId, ClientRegistrationFixture.InitialSecret));
        introspection.Status.ShouldEqual(HttpStatusCode.OK);
        introspection.Body.GetProperty("active").GetBoolean().ShouldBeTrue();
        var applications = await _fixture.Applications.Find(FilterDefinition<OpenIddict.MongoDb.Models.OpenIddictMongoDbApplication>.Empty).ToListAsync(TestContext.Current.CancellationToken);
        applications.Count.ShouldEqual(2);
        applications.TrueForAll(application => application.Id != ObjectId.Empty).ShouldBeTrue();
        applications.TrueForAll(application => application.Properties!.Contains("cratis_client_registration_id")).ShouldBeTrue();
        var facts = await _fixture.ClientFacts();
        facts.Count.ShouldBeGreaterThan(0);
        var persisted = facts.Select(document => document.ToJson()).Concat(applications.Select(application => application.ToJson())).ToArray();
        var bytes = Encoding.UTF8.GetBytes(ClientRegistrationFixture.InitialSecret);
        foreach (var forbidden in new[] { ClientRegistrationFixture.InitialSecret, Convert.ToBase64String(bytes), Convert.ToHexString(SHA256.HashData(bytes)), Convert.ToBase64String(SHA256.HashData(bytes)) })
        {
            persisted.Any(json => json.Contains(forbidden, StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        }
    }
}
