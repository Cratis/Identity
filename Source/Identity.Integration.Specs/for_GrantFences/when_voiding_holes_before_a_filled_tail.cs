// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_voiding_holes_before_a_filled_tail(IdentityHost host) : IAsyncLifetime
{
    long _blocked;
    long _complete;
    FenceRecord[] _tail;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var store = new MongoFenceStore(FenceLogDatabase.Operational(host), TimeProvider.System, log);
        var id = Guid.NewGuid();
        var grant = Guid.NewGuid().ToString("D");
        await log.Reserve(id, grant, CancellationToken.None);
        var fence = await store.Commit(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), RevocationReason.Revoked, CancellationToken.None);
        await log.Append(FenceRecordFactory.Record(await store.Read(fence.GrantId, CancellationToken.None)), CancellationToken.None);
        _blocked = await log.Watermark(CancellationToken.None);
        await new ReconcileReservation(log, store).Reconcile(new(id, 1, grant, ReservationState.Reserved, DateTimeOffset.UnixEpoch), CancellationToken.None);
        _complete = await log.Watermark(CancellationToken.None);
        _tail = (await log.Read(0, CancellationToken.None)).ToArray();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_not_advance_over_a_reserved_hole() => _blocked.ShouldEqual(0L);
    [Fact] void should_advance_over_a_proven_void_and_the_filled_tail() => _complete.ShouldEqual(2L);
    [Fact] void should_retain_the_permanent_tail_tombstone() => _tail.Single().Fence.Sequence.ShouldEqual(2L);
}
