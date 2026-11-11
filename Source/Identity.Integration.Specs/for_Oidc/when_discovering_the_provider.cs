// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcDiscovery)]
[Collection(OidcCollection.Name)]
public class when_discovering_the_provider(OidcIdentityHost host) : Specification
{
    Browser _browser;
    JsonElement _metadata;

    async Task Because()
    {
        _browser = host.CreateBrowser();
        _metadata = JsonElement.Parse(await _browser.Client.GetStringAsync("/.well-known/openid-configuration"));
    }

    [Fact] void should_publish_the_exact_issuer() => Check(() => _metadata.GetProperty("issuer").GetString().ShouldEqual(IdentityHost.Issuer));
    [Fact] void should_publish_userinfo() => Check(() => _metadata.GetProperty("userinfo_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/userinfo"));
    [Fact] void should_publish_the_authorization_endpoint() => Check(() => _metadata.GetProperty("authorization_endpoint").GetString().ShouldEqual($"{IdentityHost.Issuer}connect/authorize"));
    [Fact] void should_publish_only_rs256() => Check(() => Strings("id_token_signing_alg_values_supported").ShouldContainOnly("RS256"));
    [Fact] void should_be_code_only() => Check(() => Strings("response_types_supported").ShouldContainOnly("code"));
    [Fact] void should_use_public_subjects() => Check(() => Strings("subject_types_supported").ShouldContainOnly("public"));
    [Fact] void should_require_s256() => Check(() => Strings("code_challenge_methods_supported").ShouldContainOnly("S256"));
    [Fact] void should_publish_the_bounded_profile() => Check(() => Strings("scopes_supported").ShouldContainOnly("openid", "profile", "offline_access"));
    [Fact] void should_not_advertise_email_or_upstream_identity() => Check(() => Strings("claims_supported").ShouldNotContain("email"));

    IEnumerable<string> Strings(string name) => _metadata.GetProperty(name).EnumerateArray().Select(value => value.GetString()!);
    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcDiscovery, assertion);
    void Destroy() => _browser?.Client.Dispose();
}
