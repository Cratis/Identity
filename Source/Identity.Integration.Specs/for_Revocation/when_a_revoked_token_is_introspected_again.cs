// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Revocation;

[Collection(ChronicleOutageCollection.Name)]
public class when_a_revoked_token_is_introspected_again(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _before;
    (HttpStatusCode Status, JsonElement Body) _after;
    (HttpStatusCode Status, JsonElement Body) _whileUnavailable;
    HttpStatusCode _revocation;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var accessToken = tokens.GetProperty("access_token").GetString();

        _before = await browser.Introspect(accessToken);
        (_revocation, _) = await browser.Revoke(tokens.GetProperty("refresh_token").GetString());
        _after = await browser.Introspect(accessToken);

        await host.StopChronicle();
        try
        {
            _whileUnavailable = await browser.Introspect(accessToken);
        }
        finally
        {
            await host.RestartChronicle();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_report_the_token_active_before_revocation() => _before.Body.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_acknowledge_the_revocation() => _revocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_report_the_token_inactive_on_the_next_introspection() => _after.Body.GetProperty("active").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_report_the_token_active_while_the_revocation_source_is_unavailable() => (_whileUnavailable.Status != HttpStatusCode.OK || !_whileUnavailable.Body.GetProperty("active").GetBoolean()).ShouldBeTrue();
}
