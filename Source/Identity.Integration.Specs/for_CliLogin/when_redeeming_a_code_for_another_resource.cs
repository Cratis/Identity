// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_CliLogin;

[Collection(IdentityCollection.Name)]
public class when_redeeming_a_code_for_another_resource(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _status;
    JsonElement _body;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        (_status, _body) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier, IdentityHost.StudioMcp);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_refuse_it() => _status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_refuse_it_as_an_invalid_target() => _body.GetProperty("error").GetString().ShouldEqual("invalid_target");
}
