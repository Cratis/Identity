// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcClientAuth)]
[Collection(OidcCollection.Name)]
public class when_authenticating_the_confidential_client(OidcIdentityHost host) : Specification
{
    Browser _browser;
    HttpStatusCode _wrong;
    HttpStatusCode _missing;
    HttpStatusCode _correct;

    async Task Because()
    {
        _browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var code = await OidcRequests.Code(_browser, challenge);
        KeyValuePair<string, string>[] fields = [new("grant_type", "authorization_code"), new("client_id", SpecClientDescriptors.OidcWeb), new("code", code), new("code_verifier", verifier), new("redirect_uri", OidcRequests.RedirectUri)];
        _wrong = (await _browser.PostForm("/connect/token", fields, (SpecClientDescriptors.OidcWeb, SpecClientDescriptors.OidcClientSecret + "-incorrect"))).Status;
        _missing = (await _browser.PostForm("/connect/token", fields)).Status;
        _correct = (await OidcRequests.Redeem(_browser, code, verifier)).Status;
    }

    [Fact] void should_refuse_the_wrong_secret() => Check(() => _wrong.ShouldEqual(HttpStatusCode.Unauthorized));
    [Fact] void should_refuse_a_missing_secret() => Check(() => _missing.ShouldNotEqual(HttpStatusCode.OK));
    [Fact] void should_accept_the_registered_client_without_consuming_failed_attempts() => Check(() => _correct.ShouldEqual(HttpStatusCode.OK));

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcClientAuth, assertion);
    void Destroy() => _browser?.Client.Dispose();
}
