// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_RefreshTokens;

/// <summary>
/// The winner has redeemed the token but not yet stored its successor when the same token is presented again. The
/// reuse is caught at token validation; OpenIddict alone would revoke only the tokens that exist at that moment and
/// leave the authorization valid, so the successor stored afterwards would keep working.
/// </summary>
/// <param name="host">The <see cref="IdentityHost"/>.</param>
[Collection(IdentityCollection.Name)]
public class when_a_redeemed_refresh_token_is_reused_before_its_successor_is_issued(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _winner;
    (HttpStatusCode Status, JsonElement Body) _reuse;
    (HttpStatusCode Status, JsonElement Body) _successorRefresh;
    JsonElement _successorIntrospection;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;

        var hold = host.RefreshGates.ArmHold(refreshToken);
        var winner = Task.Run(() => browser.Refresh(refreshToken));
        try
        {
            await hold.Reached;
            _reuse = await host.CreateBrowser().Refresh(refreshToken);
        }
        finally
        {
            hold.Release();
        }

        _winner = await winner;
        if (_winner.Status == HttpStatusCode.OK)
        {
            _successorRefresh = await browser.Refresh(_winner.Body.GetProperty("refresh_token").GetString()!);
            (_, _successorIntrospection) = await browser.Introspect(_winner.Body.GetProperty("access_token").GetString()!);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_reject_the_reuse_as_an_invalid_grant() => _reuse.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_let_the_winner_finish() => _winner.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_reject_the_successor_refresh_token() => _successorRefresh.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_reject_the_successor_as_an_invalid_grant() => _successorRefresh.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_deactivate_the_successor_access_token() => _successorIntrospection.GetProperty("active").GetBoolean().ShouldBeFalse();
}
