// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcUserInfo)]
[Collection(OidcCollection.Name)]
public class when_requesting_userinfo(OidcIdentityHost host) : an_oidc_login(host)
{
    JsonElement _userInfo;
    HttpStatusCode _status;
    HttpStatusCode _missing;
    HttpStatusCode _malformed;
    HttpStatusCode _afterRevocation;
    bool _noStore;

    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        using var response = await _browser.Client.SendAsync(request);
        _status = response.StatusCode;
        _noStore = response.Headers.CacheControl?.NoStore == true;
        _userInfo = await Browser.ReadJson(response);
        using var missing = await _browser.Client.GetAsync("/connect/userinfo");
        _missing = missing.StatusCode;
        using var bad = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        bad.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");
        using var malformed = await _browser.Client.SendAsync(bad);
        _malformed = malformed.StatusCode;
        await _browser.PostForm("/connect/revocation", [new("token", _accessToken), new("token_type_hint", "access_token")], (ClientId, SpecClientDescriptors.OidcClientSecret));
        using var revoked = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        revoked.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        using var refused = await _browser.Client.SendAsync(revoked);
        _afterRevocation = refused.StatusCode;
    }

    [Fact] void should_accept_the_identity_only_bearer() => Check(() => _status.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_expose_only_sub_and_profile() => Check(() => _userInfo.EnumerateObject().Select(property => property.Name).ShouldContainOnly("sub", "name", "preferred_username"));
    [Fact] void should_prevent_storage() => Check(() => _noStore.ShouldBeTrue());
    [Fact] void should_refuse_a_revoked_access_token() => Check(() => _afterRevocation.ShouldEqual(HttpStatusCode.Unauthorized));
    [Fact] void should_require_a_bearer() => Check(() => _missing.ShouldEqual(HttpStatusCode.Unauthorized));
    [Fact] void should_refuse_a_malformed_bearer() => Check(() => _malformed.ShouldEqual(HttpStatusCode.Unauthorized));

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcUserInfo, assertion);
}
