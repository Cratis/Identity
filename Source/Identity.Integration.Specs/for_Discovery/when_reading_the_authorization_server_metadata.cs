// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Discovery;

[Collection(IdentityCollection.Name)]
public class when_reading_the_authorization_server_metadata(IdentityHost host) : IAsyncLifetime
{
    JsonElement _metadata;
    JsonElement _jwks;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        _metadata = JsonDocument.Parse(await browser.Client.GetStringAsync("/.well-known/oauth-authorization-server")).RootElement.Clone();
        _jwks = JsonDocument.Parse(await browser.Client.GetStringAsync(_metadata.GetProperty("jwks_uri").GetString())).RootElement.Clone();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_name_the_configured_issuer() => _metadata.GetProperty("issuer").GetString().ShouldEqual(IdentityHost.Issuer);
    [Fact] void should_publish_the_authorization_endpoint() => _metadata.GetProperty("authorization_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/authorize");
    [Fact] void should_publish_the_token_endpoint() => _metadata.GetProperty("token_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/token");
    [Fact] void should_publish_the_revocation_endpoint() => _metadata.GetProperty("revocation_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/revocation");
    [Fact] void should_publish_the_introspection_endpoint() => _metadata.GetProperty("introspection_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/introspect");
    [Fact] void should_only_support_s256_pkce() => Strings("code_challenge_methods_supported").ShouldContainOnly("S256");
    [Fact] void should_only_support_code_and_refresh_grants() => Strings("grant_types_supported").ShouldContainOnly("authorization_code", "refresh_token");
    [Fact] void should_list_the_resource_scopes() => Strings("scopes_supported").ShouldContain("direct:read");
    [Fact] void should_publish_a_public_signing_key() => _jwks.GetProperty("keys").EnumerateArray().Any(key => key.GetProperty("use").GetString() == "sig").ShouldBeTrue();
    [Fact] void should_not_publish_private_key_material() => _jwks.GetProperty("keys").EnumerateArray().Any(key => key.TryGetProperty("d", out _) || key.TryGetProperty("p", out _)).ShouldBeFalse();

    IEnumerable<string> Strings(string property) => _metadata.GetProperty(property).EnumerateArray().Select(_ => _.GetString()!).ToArray();
}
