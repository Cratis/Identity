// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_revocation_precedes_a_stale_redemption(IdentityHost host) : IAsyncLifetime
{
    RedeemOutcome _outcome;
    GrantState _state;
    GrantFence _first;
    GrantFence _retry;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var operational = FenceLogDatabase.Operational(host);
        var first = new MongoFenceStore(operational, TimeProvider.System, log);
        var second = new MongoFenceStore(operational, TimeProvider.System, log);
        var grant = Guid.NewGuid().ToString("D");
        var subject = Guid.NewGuid().ToString("D");
        var read = await first.RecordFamilyStart(new FamilyStart(grant, "presented-entry", subject, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)), CancellationToken.None);
        _first = await second.Commit(grant, subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _outcome = await first.RedeemAndInstall(grant, "presented-entry", "successor-entry", read.Version, CancellationToken.None);
        _retry = await new MongoFenceStore(operational, TimeProvider.System, log).Commit(grant, subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        _state = await first.Read(grant, CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_reject_on_the_other_instance() => _outcome.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_not_install_a_successor() => _state.CurrentRefreshTokenId.ShouldBeNull();
    [Fact] void should_preserve_the_fence_across_new_store_instances() => _retry.ShouldEqual(_first);
    [Fact] void should_write_all_intents_with_the_restriction() => _state.Outbox.Length.ShouldEqual(4);
}
