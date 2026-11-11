// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcUserInfoSubMatch)]
[Collection(OidcCollection.Name)]
public class when_matching_userinfo_to_an_openid_only_token(OidcIdentityHost host) : an_oidc_login(host)
{
    protected override string ClientId => SpecClientDescriptors.OidcOpenIdOnly;
    protected override string Scope => "openid";
    JsonElement _userInfo;
    HttpStatusCode _status;

    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        using var response = await _browser.Client.SendAsync(request);
        _status = response.StatusCode;
        _userInfo = await Browser.ReadJson(response);
    }

    [Fact] void should_accept_post_with_a_bearer() => Check(() => _status.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_match_the_id_token_subject() => Check(() => _userInfo.GetProperty("sub").GetString().ShouldEqual(_identity.GetProperty("sub").GetString()));
    [Fact] void should_return_only_sub_without_profile() => Check(() => _userInfo.EnumerateObject().Select(property => property.Name).ShouldContainOnly("sub"));
    [Fact] void should_not_upgrade_the_id_token_to_profile() => Check(() => _identity.TryGetProperty("preferred_username", out _).ShouldBeFalse());

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcUserInfoSubMatch, assertion);
}
