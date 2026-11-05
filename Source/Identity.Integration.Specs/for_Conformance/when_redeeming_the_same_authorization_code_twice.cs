// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Conformance;

[Requirement(Requirements.CodeSingleUse)]
[Collection(IdentityCollection.Name)]
public class when_redeeming_the_same_authorization_code_twice(IdentityHost host) : Specification
{
    Browser _browser;
    HttpStatusCode _firstStatus;
    HttpStatusCode _replayStatus;
    JsonElement _error;

    async Task Establish()
    {
        _browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var code = await _browser.LoginForCode(challenge);
        using var first = await _browser.RedeemCodeResponse(code, verifier);
        _firstStatus = first.StatusCode;
        using var replay = await _browser.RedeemCodeResponse(code, verifier);
        _replayStatus = replay.StatusCode;
        _error = await Browser.ReadJson(replay);
    }

    [Fact] void should_accept_the_first_redemption() => Conformance.Conformance.Expect(Requirements.CodeSingleUse, () => _firstStatus.ShouldEqual(HttpStatusCode.OK));
    [Fact] void should_reject_the_replay() => Conformance.Conformance.Expect(Requirements.CodeSingleUse, () => _replayStatus.ShouldEqual(HttpStatusCode.BadRequest));
    [Fact] void should_report_invalid_grant() => Conformance.Conformance.Expect(Requirements.CodeSingleUse, () => _error.GetProperty("error").GetString().ShouldEqual("invalid_grant"));

    void Destroy() => _browser.Client.Dispose();
}
