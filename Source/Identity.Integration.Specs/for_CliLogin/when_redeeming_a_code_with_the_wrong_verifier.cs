// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_CliLogin;

[Requirement(Requirements.PkceVerifierBound)]
[Collection(IdentityCollection.Name)]
public class when_redeeming_a_code_with_the_wrong_verifier(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _status;
    JsonElement _body;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var (otherVerifier, _) = Browser.Pkce();
        (_status, _body) = await browser.RedeemCode(await browser.LoginForCode(challenge), otherVerifier);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_refuse_it() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_refuse_it_as_an_invalid_grant() => _body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
}
