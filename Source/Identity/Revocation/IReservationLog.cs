// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// The independent, conditional reservation transitions used by reconciliation.
/// </summary>
internal interface IReservationLog
{
    /// <summary>Reads at most 100 unresolved reservations, oldest sequence first.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The unresolved reservations.</returns>
    Task<IReadOnlyList<FenceReservation>> Unresolved(CancellationToken cancellationToken);

    /// <summary>Fences new appends, without assuming the operational writer stopped.</summary>
    /// <param name="reservation">The reservation.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The current reservation state.</returns>
    Task<ReservationState> Fence(FenceReservation reservation, CancellationToken cancellationToken);

    /// <summary>Resolves a fenced reservation only after the operational cancellation/commit decision.</summary>
    /// <param name="reservation">The fenced reservation.</param>
    /// <param name="committed">The committed record, or null after proven cancellation.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable resolution.</returns>
    Task Resolve(FenceReservation reservation, FenceRecord? committed, CancellationToken cancellationToken);
}
