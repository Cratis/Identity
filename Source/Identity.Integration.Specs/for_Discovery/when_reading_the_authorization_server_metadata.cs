// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Discovery;

[Requirement(Requirements.PkceS256Only), Requirement(Requirements.AsMetadata), Requirement(Requirements.JwksPublicOnly)]
[Collection(IdentityCollection.Name)]
public class when_reading_the_authorization_server_metadata(IdentityHost host) : IAsyncLifetime
{
    JsonElement _metadata;
    JsonElement _jwks;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        _metadata = JsonElement.Parse(await browser.Client.GetStringAsync("/.well-known/oauth-authorization-server"));
        _jwks = JsonElement.Parse(await browser.Client.GetStringAsync(_metadata.GetProperty("jwks_uri").GetString()));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_name_the_configured_issuer() => _metadata.GetProperty("issuer").GetString().ShouldEqual(IdentityHost.Issuer);
    [Fact] void should_publish_the_authorization_endpoint() => _metadata.GetProperty("authorization_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/authorize");
    [Fact] void should_publish_the_token_endpoint() => _metadata.GetProperty("token_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/token");
    [Fact] void should_advertise_the_issuer_in_authorization_responses() => _metadata.GetProperty("authorization_response_iss_parameter_supported").GetBoolean().ShouldBeTrue();
    [Fact] void should_publish_the_revocation_endpoint() => _metadata.GetProperty("revocation_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/revocation");
    [Fact] void should_publish_the_introspection_endpoint() => _metadata.GetProperty("introspection_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/introspect");
    [Fact] void should_only_support_s256_pkce() => Strings("code_challenge_methods_supported").ShouldContainOnly("S256");
    [Fact] void should_support_device_grants_when_explicitly_enabled() => Strings("grant_types_supported").ShouldContainOnly("authorization_code", "refresh_token", "urn:ietf:params:oauth:grant-type:device_code");
    [Fact] void should_list_the_resource_scopes() => Strings("scopes_supported").ShouldContain("direct:read");
    [Fact] void should_not_offer_openid_connect_sign_in() => Strings("scopes_supported").ShouldNotContain("openid");
    [Fact] void should_publish_a_public_signing_key() => _jwks.GetProperty("keys").EnumerateArray().Any(key => key.GetProperty("use").GetString() == "sig").ShouldBeTrue();
    [Fact] void should_not_publish_private_key_material() => _jwks.GetProperty("keys").EnumerateArray().Any(key => key.TryGetProperty("d", out _) || key.TryGetProperty("p", out _)).ShouldBeFalse();

    [Fact] void should_only_support_the_code_response_type() => Strings("response_types_supported").ShouldContainOnly("code");
    [Fact] void should_not_publish_any_private_key_parameters() => _jwks.GetProperty("keys").EnumerateArray().SelectMany(_ => _.EnumerateObject()).Any(_ => new[] { "d", "p", "q", "dp", "dq", "qi", "oth", "k" }.Contains(_.Name, StringComparer.Ordinal)).ShouldBeFalse();

    IEnumerable<string> Strings(string property) => _metadata.GetProperty(property).EnumerateArray().Select(_ => _.GetString()!).ToArray();
}
