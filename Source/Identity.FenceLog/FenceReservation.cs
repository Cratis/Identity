// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>
/// An independently allocated reservation. The identity is also the operational cancellation key.
/// </summary>
/// <param name="Id">The stable fence identity.</param>
/// <param name="Sequence">The independently allocated sequence.</param>
/// <param name="GrantId">The authorization.</param>
/// <param name="State">The log-side writer fence.</param>
/// <param name="ReservedAt">The reservation time.</param>
public sealed record FenceReservation(Guid Id, long Sequence, string GrantId, ReservationState State, DateTimeOffset ReservedAt);
