// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>Internal source of UpstreamKeyUnlinked; does not release the reservation. See the #26 catalogue.</summary>
/// <param name="LinkId">The link.</param>
/// <param name="KeyHash">The reserved pseudonym.</param>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="UnlinkedAt">The unlink time.</param>
[EventType("de708442-c9b3-4d8b-b677-01450ef9248e", generation: 1)]
public record UpstreamKeyUnlinkRecorded(LinkId LinkId, UpstreamLookupDigest KeyHash, ReasonId ReasonId, DateTimeOffset UnlinkedAt);
