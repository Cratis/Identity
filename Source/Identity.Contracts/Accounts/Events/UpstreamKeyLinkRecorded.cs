// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of UpstreamKeyLinked. See the #26 catalogue.</summary>
/// <param name="LinkId">The link.</param>
/// <param name="ConnectionId">The connection.</param>
/// <param name="IdpIssuer">The exact authority.</param>
/// <param name="KeyHash">The protected lookup pseudonym.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="LinkedAt">The link time.</param>
[EventType("0948769e-d249-472e-abb3-93e8743379da", generation: 1)]
public record UpstreamKeyLinkRecorded(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamLookupDigest KeyHash, EvidenceId EvidenceId, DateTimeOffset LinkedAt);
