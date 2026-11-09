// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A real failed erasure workflow transition; no error payload. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="ErasureId">The request reference.</param>
/// <param name="StageId">The stage that failed.</param>
/// <param name="ReasonId">The allowlisted failure reason.</param>
[EventType("af2d3634-9adc-48eb-b00d-6565df2a38e8", generation: 1)]
public record AccountErasureFailed(ErasureId ErasureId, ErasureStageId StageId, ReasonId ReasonId);
