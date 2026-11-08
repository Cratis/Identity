// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_a_delayed_writer_arrives_after_void(IdentityHost host) : IAsyncLifetime
{
    Exception? _refusal;
    long _prefix;
    FenceRecord? _record;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var writer = new MongoFenceStore(FenceLogDatabase.Operational(host), TimeProvider.System, log);
        var id = Guid.NewGuid();
        var grant = Guid.NewGuid().ToString("D");
        var sequence = await log.Reserve(id, grant, CancellationToken.None);
        var reservation = new FenceReservation(id, sequence, grant, ReservationState.Reserved, DateTimeOffset.UnixEpoch);
        await new ReconcileReservation(log, writer).Reconcile(reservation, CancellationToken.None);
        var fence = new GrantFence(id, sequence, grant, Guid.NewGuid().ToString("D"), RevocationReason.Revoked, DateTimeOffset.UnixEpoch);
        _refusal = await Catch.Exception(() => writer.CommitReserved(null, fence, CancellationToken.None));
        _record = await log.Tombstone(grant, CancellationToken.None);
        _prefix = await log.Watermark(CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_refuse_the_delayed_operational_commit() => _refusal.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_keep_the_reservation_voided() => _record.ShouldBeNull();
    [Fact] void should_allow_only_the_voided_prefix() => _prefix.ShouldEqual(1L);
}
