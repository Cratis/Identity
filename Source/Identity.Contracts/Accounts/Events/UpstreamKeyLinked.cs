// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A verified key was linked atomically. See the #26 identity-events-v1 catalogue.</summary>
/// <remarks>Raw IdpSub belongs exclusively to the Internal UpstreamKeySubjectRecorded companion.</remarks>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="ConnectionId">The connection.</param>
/// <param name="IdpIssuer">The exact upstream authority.</param>
/// <param name="KeyHash">The protected keyed lookup pseudonym.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="LinkedAt">The link time.</param>
[EventType("e2d1b4ad-914e-41c6-9187-c589fb36d68c", generation: 1), Public]
public record UpstreamKeyLinked(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamLookupDigest KeyHash, EvidenceId EvidenceId, DateTimeOffset LinkedAt);
