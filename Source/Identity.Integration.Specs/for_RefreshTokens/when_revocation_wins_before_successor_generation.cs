// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_RefreshTokens;

[Collection(IdentityCollection.Name)]
public class when_revocation_wins_before_successor_generation(IdentityHost host) : IAsyncLifetime
{
    readonly List<HttpStatusCode> _revocations = [];
    readonly List<(HttpStatusCode Status, JsonElement Body)> _refreshes = [];

    public async ValueTask InitializeAsync()
    {
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var browser = host.CreateBrowser();
            var (verifier, challenge) = Browser.Pkce();
            var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
            var presented = tokens.GetProperty("refresh_token").GetString();

            // Revoke must land before consume/install, not merely before the successor's token entry is inserted.
            var hold = host.RefreshGates.ArmBeforeRedemptionHold(presented);
            var refresh = browser.Refresh(presented);
            try
            {
                await hold.Reached;
                var (status, _) = await browser.Revoke(presented);
                _revocations.Add(status);
            }
            finally
            {
                hold.Release();
            }

            _refreshes.Add(await refresh);
            _refreshes.Add(await browser.Refresh(presented));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_acknowledge_all_fifty_restrictions() => _revocations.Count(status => status == HttpStatusCode.OK).ShouldEqual(50);
    [Fact] void should_reject_every_refresh_ordered_after_the_fence() => _refreshes.TrueForAll(response => response.Status == HttpStatusCode.BadRequest && response.Body.GetProperty("error").GetString() == "invalid_grant").ShouldBeTrue();
    [Fact] void should_not_release_a_successor() => _refreshes.Exists(response => response.Body.TryGetProperty("refresh_token", out _)).ShouldBeFalse();
    [Fact] void should_not_release_an_access_token() => _refreshes.Exists(response => response.Body.TryGetProperty("access_token", out _)).ShouldBeFalse();
}
