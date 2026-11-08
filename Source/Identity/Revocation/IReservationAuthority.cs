// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// The operational ordering point shared by cancellation and the writer's fence/outbox transaction.
/// </summary>
internal interface IReservationAuthority
{
    /// <summary>
    /// Atomically wins cancellation, or returns the record committed by the winning writer.
    /// Uncertain outcomes throw; they must never be interpreted as cancellation.
    /// </summary>
    /// <param name="reservation">The independently allocated reservation.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Null only after durable cancellation, otherwise the committed record.</returns>
    Task<FenceRecord?> Cancel(FenceReservation reservation, CancellationToken cancellationToken);
}
