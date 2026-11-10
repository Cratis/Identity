// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>The approved erasure workflow boundary is complete. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="ErasureId">The request reference.</param>
/// <param name="CompletedAt">The completion time.</param>
/// <param name="BoundaryVersion">The approved completion boundary version.</param>
[EventType, Public]
public record AccountErased(ErasureId ErasureId, DateTimeOffset CompletedAt, BoundaryVersion BoundaryVersion);
