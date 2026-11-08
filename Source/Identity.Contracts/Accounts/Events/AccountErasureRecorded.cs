// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of AccountErased. See the #26 catalogue.</summary>
/// <param name="ErasureId">The request reference.</param>
/// <param name="CompletedAt">The completion time.</param>
/// <param name="BoundaryVersion">The approved completion boundary version.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType("213861f1-a227-4f67-acae-a0619f4eb946", generation: 1)]
public record AccountErasureRecorded(ErasureId ErasureId, DateTimeOffset CompletedAt, BoundaryVersion BoundaryVersion, FactId FactId);
