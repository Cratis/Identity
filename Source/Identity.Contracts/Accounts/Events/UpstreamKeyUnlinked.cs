// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A link was removed; its key remains reserved.</summary>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="KeyHash">The reserved keyed pseudonym.</param>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="UnlinkedAt">The unlink time.</param>
[EventType, Public]
public record UpstreamKeyUnlinked(LinkId LinkId, UpstreamLookupDigest KeyHash, ReasonId ReasonId, DateTimeOffset UnlinkedAt);
