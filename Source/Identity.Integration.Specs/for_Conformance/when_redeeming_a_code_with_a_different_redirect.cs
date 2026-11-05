// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.CodeRedirectBound)]
[Collection(IdentityCollection.Name)]
public class when_redeeming_a_code_with_a_different_redirect(IdentityHost host) : Specification
{
    Browser _browser;
    HttpStatusCode _status;
    JsonElement _error;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var code = await _browser.LoginForCode(challenge);
        using var response = await _browser.RedeemCodeResponse(code, verifier, "http://127.0.0.1:49153/callback");
        _status = response.StatusCode;
        _error = await Browser.ReadJson(response);
    }

    [Fact] void should_reject_a_changed_loopback_port_at_redemption() => Conformance.Conformance.Expect(Requirements.CodeRedirectBound, () => _status.ShouldEqual(HttpStatusCode.BadRequest));
    [Fact] void should_report_invalid_grant() => Conformance.Conformance.Expect(Requirements.CodeRedirectBound, () => _error.GetProperty("error").GetString().ShouldEqual("invalid_grant"));

    void Destroy() => _browser.Client.Dispose();
}
