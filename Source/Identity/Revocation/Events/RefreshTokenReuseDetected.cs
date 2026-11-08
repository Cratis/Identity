// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation.Events;

/// <summary>A meaningful compromise fact, with no token entry or submitted value.</summary>
/// <param name="GrantId">The grant.</param>
/// <param name="DetectionId">The persisted observation identity.</param>
/// <param name="DetectedAt">The observation time.</param>
/// <param name="FactId">The retry-stable fact.</param>
[EventType("bedbc6d5-2870-4cf7-aa5e-79e7a2bf3cb6", generation: 1)]
internal sealed record RefreshTokenReuseDetected(GrantId GrantId, DetectionId DetectionId, DateTimeOffset DetectedAt, FactId FactId);
