// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Collection(IdentityCollection.Name)]
public class when_discovery_is_disabled(IdentityHost host) : Specification
{
    Browser _browser;
    JsonElement _provider;
    JsonElement _oauth;
    HttpStatusCode _userInfo;

    async Task Because()
    {
        _browser = host.CreateBrowser();
        _provider = JsonDocument.Parse(await _browser.Client.GetStringAsync("/.well-known/openid-configuration")).RootElement.Clone();
        _oauth = JsonDocument.Parse(await _browser.Client.GetStringAsync("/.well-known/oauth-authorization-server")).RootElement.Clone();
        using var userInfo = await _browser.Client.GetAsync("/connect/userinfo");
        _userInfo = userInfo.StatusCode;
    }

    [Fact] void should_publish_only_issuer_and_keys() => _provider.EnumerateObject().Select(property => property.Name).ShouldContainOnly("issuer", "jwks_uri");
    [Fact] void should_not_upgrade_oauth_to_openid() => _oauth.GetProperty("scopes_supported").EnumerateArray().Select(scope => scope.GetString()).ShouldNotContain("openid");
    [Fact] void should_not_map_userinfo() => _userInfo.ShouldEqual(HttpStatusCode.NotFound);
    void Destroy() => _browser?.Client.Dispose();
}
