// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Replay.given;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Replay.for_Revocation;

public class when_replaying_after_restore_of_a_stale_chronicle : RevocationSpecs.given.a_recovery_pass
{
    FenceReservation _voided;

    void Establish()
    {
        _contains = false;
        _options.RestoreKind = RestoreKind.Chronicle;
        _options.RestoreId = "stale-chronicle-replay";
        _voided = new(Guid.NewGuid(), 2, "cancelled-grant", ReservationState.Voided, DateTimeOffset.UnixEpoch);
        _log.Voided(Arg.Any<CancellationToken>()).Returns([_voided]);
        _authority.Cancel(_voided, Arg.Any<CancellationToken>()).Returns((FenceRecord?)null);
        _watermark = _allocated = 2;
        var pending = new GrantState(_record.Fence.GrantId, 1, null, _record.Fence, _record.Intents.Select(entry => entry with { State = OutboxState.LogConfirmed }).ToArray());
        _fences.Undelivered(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([pending]);
        _fences.Read(pending.GrantId, Arg.Any<CancellationToken>()).Returns(pending);
        _restrictionFacts.Append(Arg.Any<OutboxEntry>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new RevocationPending()));
    }

    async Task Because()
    {
        await new ReadModelRebuild().Rebuild();
        await Run();
        await new ReadModelRebuild().Rebuild();
    }

    [Fact] void should_reapply_the_tombstone_without_resetting_it() => _applied.ShouldEqual([(_record, true)]);
    [Fact] void should_keep_the_delayed_writer_cancelled() => _authority.Received(1).Cancel(_voided, Arg.Any<CancellationToken>());
    [Fact] void should_keep_issuance_closed() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_keep_revoked_facts_undelivered() => _fences.DidNotReceive().Delivered(_record.Fence.GrantId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_leave_recovery_pending() => _failure.ShouldBeOfExactType<RevocationPending>();
}
