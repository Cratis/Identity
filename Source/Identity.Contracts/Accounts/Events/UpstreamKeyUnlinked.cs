// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A link was removed; its key remains reserved. See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkId">The confirmed link.</param>
/// <param name="KeyHash">The reserved keyed pseudonym.</param>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="UnlinkedAt">The unlink time.</param>
[EventType("f84b03e3-e0c1-4593-962e-917c56e50bcc", generation: 1), Public]
public record UpstreamKeyUnlinked(LinkId LinkId, UpstreamLookupDigest KeyHash, ReasonId ReasonId, DateTimeOffset UnlinkedAt);
