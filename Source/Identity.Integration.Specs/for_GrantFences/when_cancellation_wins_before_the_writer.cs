// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_cancellation_wins_before_the_writer(IdentityHost host) : IAsyncLifetime
{
    Exception? _refusal;
    GrantState? _state;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var operational = FenceLogDatabase.Operational(host);
        var writer = new MongoFenceStore(operational, TimeProvider.System, log);
        var reconciler = new MongoFenceStore(operational, TimeProvider.System, log);
        var id = Guid.NewGuid();
        var grant = Guid.NewGuid().ToString("D");
        var sequence = await log.Reserve(id, grant, CancellationToken.None);
        var reservation = new FenceReservation(id, sequence, grant, ReservationState.Reserved, DateTimeOffset.UnixEpoch);
        await log.Fence(reservation, CancellationToken.None);
        await reconciler.Cancel(reservation, CancellationToken.None);
        var fence = new GrantFence(id, sequence, grant, Guid.NewGuid().ToString("D"), RevocationReason.Revoked, DateTimeOffset.UnixEpoch);
        _refusal = await Catch.Exception(() => writer.CommitReserved(null, fence, CancellationToken.None));
        _state = await writer.Read(grant, CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_refuse_the_operational_commit() => _refusal.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_install_a_fence_after_cancellation() => _state.ShouldBeNull();
}
