// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_RefreshTokens;

[Collection(IdentityCollection.Name)]
public class when_refresh_wins_before_revocation(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _revocation;
    (HttpStatusCode Status, JsonElement Body) _winner;
    (HttpStatusCode Status, JsonElement Body) _successor;
    (HttpStatusCode Status, JsonElement Body) _presented;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var presented = tokens.GetProperty("refresh_token").GetString();

        // This hold follows the atomic consume/install write, but precedes successor generation.
        var hold = host.RefreshGates.ArmHold(presented);
        var refresh = browser.Refresh(presented);
        try
        {
            await hold.Reached;
            (_revocation, _) = await browser.Revoke(presented);
        }
        finally
        {
            hold.Release();
        }

        _winner = await refresh;
        _successor = await browser.Refresh(_winner.Body.GetProperty("refresh_token").GetString());
        _presented = await browser.Refresh(presented);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_acknowledge_the_restriction() => _revocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_finish_the_refresh_ordered_before_the_fence() => _winner.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_the_already_admitted_access_token() => _winner.Body.GetProperty("access_token").GetString().ShouldNotBeEmpty();
    [Fact] void should_deny_the_successors_next_use() => _successor.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_reject_the_successor_as_an_invalid_grant() => _successor.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_deny_the_consumed_tokens_next_use() => _presented.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_reject_the_consumed_token_as_an_invalid_grant() => _presented.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
}
