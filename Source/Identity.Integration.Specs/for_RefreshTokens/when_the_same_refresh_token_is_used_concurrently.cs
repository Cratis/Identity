// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_RefreshTokens;

[Collection(IdentityCollection.Name)]
public class when_the_same_refresh_token_is_used_concurrently(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body)[] _results = [];
    JsonElement _originalIntrospection;
    (HttpStatusCode Status, JsonElement Body) _successorRefresh;
    JsonElement _successorIntrospection;

    (HttpStatusCode Status, JsonElement Body) Winner => _results.Single(_ => _.Status == HttpStatusCode.OK);

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;
        var secondBrowser = host.CreateBrowser();

        // Both requests pass token validation and wait just before redemption until the other has too, so both
        // genuinely race for the redemption: MongoDB's optimistic update, not an in-process lock, picks the winner.
        using (host.RefreshGates.ArmBarrier(refreshToken, participants: 2))
        {
            _results = await Task.WhenAll(
                Task.Run(() => browser.Refresh(refreshToken)),
                Task.Run(() => secondBrowser.Refresh(refreshToken)));
        }

        (_, _originalIntrospection) = await browser.Introspect(tokens.GetProperty("access_token").GetString()!);
        if (_results.Count(_ => _.Status == HttpStatusCode.OK) == 1)
        {
            _successorRefresh = await browser.Refresh(Winner.Body.GetProperty("refresh_token").GetString()!);
            (_, _successorIntrospection) = await browser.Introspect(Winner.Body.GetProperty("access_token").GetString()!);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_issue_tokens_to_exactly_one_request() => _results.Count(_ => _.Status == HttpStatusCode.OK).ShouldEqual(1);
    [Fact] void should_reject_the_other_request() => _results.Count(_ => _.Status == HttpStatusCode.BadRequest).ShouldEqual(1);
    [Fact] void should_reject_it_as_an_invalid_grant() => _results.Single(_ => _.Status == HttpStatusCode.BadRequest).Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_revoke_the_original_access_token() => _originalIntrospection.GetProperty("active").GetBoolean().ShouldBeFalse();
    [Fact] void should_reject_the_winners_successor_refresh_token() => _successorRefresh.Status.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_reject_the_successor_as_an_invalid_grant() => _successorRefresh.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_deactivate_the_winners_successor_access_token() => _successorIntrospection.GetProperty("active").GetBoolean().ShouldBeFalse();
}
