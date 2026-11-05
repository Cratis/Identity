// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.TokenResponseNoStore)]
[Collection(IdentityCollection.Name)]
public class when_reading_a_successful_token_response(IdentityHost host) : Specification
{
    Browser _browser;
    HttpResponseMessage _response;
    JsonElement _tokens;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var code = await _browser.LoginForCode(challenge);
        _response = await _browser.RedeemCodeResponse(code, verifier);
        _tokens = await Browser.ReadJson(_response);
    }

    [Fact] void should_succeed() => Conformance.Conformance.Expect(Requirements.TokenResponseNoStore, () => _response.StatusCode.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_prevent_storage() => Conformance.Conformance.Expect(Requirements.TokenResponseNoStore, () => (_response.Headers.CacheControl?.NoStore).ShouldEqual(true));
    [Fact] void should_prevent_caching_by_legacy_caches() => Conformance.Conformance.Expect(Requirements.TokenResponseNoStore, () => _response.Headers.Pragma.Select(_ => _.Name).ShouldContain("no-cache"));
    [Fact] void should_publish_the_bearer_token_type() => Conformance.Conformance.Expect(Requirements.TokenResponseNoStore, () => _tokens.GetProperty("token_type").GetString().ShouldEqual("Bearer"));
    [Fact] void should_publish_a_positive_lifetime() => Conformance.Conformance.Expect(Requirements.TokenResponseNoStore, () => _tokens.GetProperty("expires_in").GetInt32().ShouldBeGreaterThan(0));

    void Destroy()
    {
        _response?.Dispose();
        _browser.Client.Dispose();
    }
}
