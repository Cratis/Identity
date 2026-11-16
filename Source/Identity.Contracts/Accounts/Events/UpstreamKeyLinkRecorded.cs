// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of UpstreamKeyLinked.</summary>
/// <param name="LinkId">The link.</param>
/// <param name="ConnectionId">The connection.</param>
/// <param name="IdpIssuer">The exact authority.</param>
/// <param name="KeyHash">The protected lookup pseudonym.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="LinkedAt">The link time.</param>
/// <param name="FactId">The retry-stable fact identity; not present in the Public payload.</param>
[EventType]
public record UpstreamKeyLinkRecorded(LinkId LinkId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamLookupDigest KeyHash, EvidenceId EvidenceId, DateTimeOffset LinkedAt, FactId FactId);
