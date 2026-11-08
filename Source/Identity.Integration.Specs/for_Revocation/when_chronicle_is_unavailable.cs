// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Revocation;

[Collection(IdentityCollection.Name)]
public class when_chronicle_is_unavailable(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _revocation;
    HttpStatusCode _refresh;
    JsonElement _response;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        await host.StopChronicle();
        try
        {
            (_revocation, _) = await browser.Revoke(tokens.GetProperty("refresh_token").GetString());
            (_refresh, _response) = await browser.Refresh(tokens.GetProperty("refresh_token").GetString());
        }
        finally
        {
            await host.RestartChronicle();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_acknowledge_without_chronicle() => _revocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_deny_online_refresh() => _refresh.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_return_invalid_grant() => _response.GetProperty("error").GetString().ShouldEqual("invalid_grant");
}
