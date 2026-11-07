// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_the_writer_wins_cancellation(IdentityHost host) : IAsyncLifetime
{
    FenceRecord? _record;
    long _prefix;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host.Database);
        var writer = new MongoFenceStore(host.Database, TimeProvider.System, log);
        var reconciler = new MongoFenceStore(host.Database, TimeProvider.System, log);
        var id = Guid.NewGuid();
        var grant = Guid.NewGuid().ToString("D");
        var sequence = await log.Reserve(id, grant, CancellationToken.None);
        var fence = new GrantFence(id, sequence, grant, Guid.NewGuid().ToString("D"), RevocationReason.Revoked, DateTimeOffset.UnixEpoch);
        await writer.CommitReserved(null, fence, CancellationToken.None);
        await new ReconcileReservation(log, reconciler).Reconcile(new(id, sequence, grant, ReservationState.Reserved, DateTimeOffset.UnixEpoch), CancellationToken.None);
        _record = await log.Tombstone(grant, CancellationToken.None);
        _prefix = await log.Watermark(CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_fill_rather_than_void_the_committed_fence() => _record.ShouldNotBeNull();
    [Fact] void should_advance_the_proven_prefix() => _prefix.ShouldEqual(1L);
}
