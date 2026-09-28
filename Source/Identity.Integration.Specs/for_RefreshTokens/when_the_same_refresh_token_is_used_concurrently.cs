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

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;
        var secondBrowser = host.CreateBrowser();

        // Both independent clients are ready before either sends its request; MongoDB's optimistic update,
        // not an in-process lock, decides which one may redeem the token.
        var ready = new CountdownEvent(2);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<(HttpStatusCode Status, JsonElement Body)> Start(Browser contender) => await Task.Run(async () =>
        {
            ready.Signal();
            await release.Task;
            return await contender.Refresh(refreshToken);
        });

        var first = Start(browser);
        var second = Start(secondBrowser);
        ready.Wait(TimeSpan.FromSeconds(15)).ShouldBeTrue();
        release.SetResult();
        _results = await Task.WhenAll(first, second);

        (_, _originalIntrospection) = await browser.Introspect(tokens.GetProperty("access_token").GetString()!);
        var winner = _results.SingleOrDefault(_ => _.Status == HttpStatusCode.OK);
        if (winner.Status == HttpStatusCode.OK)
        {
            _successorRefresh = await browser.Refresh(winner.Body.GetProperty("refresh_token").GetString()!);
            (_, _successorIntrospection) = await browser.Introspect(winner.Body.GetProperty("access_token").GetString()!);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_allow_at_most_one_redemption() => (_results.Count(_ => _.Status == HttpStatusCode.OK) <= 1).ShouldBeTrue();
    [Fact] void should_reject_the_loser_with_invalid_grant() => _results.Where(_ => _.Status != HttpStatusCode.OK).All(_ => _.Status == HttpStatusCode.BadRequest && _.Body.GetProperty("error").GetString() == "invalid_grant").ShouldBeTrue();
    [Fact] void should_revoke_the_original_access_token() => _originalIntrospection.GetProperty("active").GetBoolean().ShouldBeFalse();
    [Fact] void should_reject_any_successor_refresh_token() => (_successorRefresh.Status == default || _successorRefresh.Status == HttpStatusCode.BadRequest).ShouldBeTrue();
    [Fact] void should_deactivate_any_successor_access_token() => (_successorIntrospection.ValueKind == JsonValueKind.Undefined || !_successorIntrospection.GetProperty("active").GetBoolean()).ShouldBeTrue();
}
