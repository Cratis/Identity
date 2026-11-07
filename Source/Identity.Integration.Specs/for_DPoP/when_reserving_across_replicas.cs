// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.DPoP.Operational;

namespace Cratis.Identity.Integration.for_DPoP;

[Collection(DPoPCollection.Name)]
[Requirement(Requirements.DPoPReplay)]
public class when_reserving_across_replicas(DPoPHost host)
{
    [Fact]
    public async Task should_admit_exactly_one_concurrent_request_across_store_instances()
    {
        var one = new MongoDPoPReplayStore(host.Database, TimeProvider.System);
        var two = new MongoDPoPReplayStore(host.Database, TimeProvider.System);
        var jti = Guid.NewGuid().ToString("N");
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => (index % 2 == 0 ? one : two).TryReserve("as", IdentityHost.Issuer, "key", jti, DateTimeOffset.UtcNow.AddSeconds(390), TestContext.Current.CancellationToken)));
        results.Count(result => result).ShouldEqual(1);
    }

    [Fact]
    public async Task should_replace_an_expired_document_even_before_the_ttl_reaper_runs()
    {
        var clock = new Clock();
        var one = new MongoDPoPReplayStore(host.Database, clock);
        var two = new MongoDPoPReplayStore(host.Database, clock);
        var jti = Guid.NewGuid().ToString("N");
        (await one.TryReserve("as", IdentityHost.Issuer, "key", jti, clock.Now.AddSeconds(390), TestContext.Current.CancellationToken)).ShouldBeTrue();
        clock.Now = clock.Now.AddSeconds(390);
        var results = await Task.WhenAll(one.TryReserve("as", IdentityHost.Issuer, "key", jti, clock.Now.AddSeconds(390), TestContext.Current.CancellationToken), two.TryReserve("as", IdentityHost.Issuer, "key", jti, clock.Now.AddSeconds(390), TestContext.Current.CancellationToken));
        results.Count(result => result).ShouldEqual(1);
    }

    sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow.AddDays(1);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
