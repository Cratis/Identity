// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_redemption_precedes_revocation(IdentityHost host) : IAsyncLifetime
{
    RedeemOutcome _installed;
    RedeemOutcome _later;
    GrantState _state;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var first = new MongoFenceStore(host.Database, TimeProvider.System, log);
        var second = new MongoFenceStore(host.Database, TimeProvider.System, log);
        var grant = Guid.NewGuid().ToString("D");
        var subject = Guid.NewGuid().ToString("D");
        var read = await first.RecordFamilyStart(grant, "presented-entry", subject, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), CancellationToken.None);
        _installed = await first.RedeemAndInstall(grant, "presented-entry", "successor-entry", read.Version, CancellationToken.None);
        await second.Commit(grant, subject, RevocationReason.Revoked, CancellationToken.None);
        _state = await first.Read(grant, CancellationToken.None);
        _later = await first.RedeemAndInstall(grant, "successor-entry", "later-entry", _state.Version, CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_install_the_one_successor_before_revoke() => _installed.ShouldEqual(RedeemOutcome.Installed);
    [Fact] void should_reject_the_successors_next_use_on_another_instance() => _later.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_leave_no_admitted_entry() => _state.CurrentRefreshTokenId.ShouldBeNull();
}
