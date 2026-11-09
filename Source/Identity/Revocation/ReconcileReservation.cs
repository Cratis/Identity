// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// A log-side fence alone cannot cancel a writer on another deployment.
/// Only a durable operational cancellation permits voiding the reservation.
/// </summary>
/// <param name="log">The independent reservation authority.</param>
/// <param name="authority">The operational cancellation/commit ordering point.</param>
internal sealed class ReconcileReservation(IReservationLog log, IReservationAuthority authority)
{
    /// <summary>Resolves one reservation without turning uncertainty into a hole that can be skipped.</summary>
    /// <param name="reservation">The unresolved reservation.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The reconciliation.</returns>
    /// <exception cref="RevocationPending">The reservation state remains uncertain.</exception>
    internal async Task Reconcile(FenceReservation reservation, CancellationToken cancellationToken)
    {
        var state = await log.Fence(reservation, cancellationToken);
        if (state is ReservationState.Filled or ReservationState.Voided)
        {
            return;
        }

        if (state != ReservationState.Fencing)
        {
            throw new RevocationPending();
        }

        var committed = await authority.Cancel(reservation, cancellationToken);
        await log.Resolve(reservation, committed, cancellationToken);
    }
}
