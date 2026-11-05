// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A link entered quarantine (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ReasonId">The reason reference.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="HoldNotBefore">The minimum hold boundary.</param>
[EventType("a407f6de-7435-47b3-bcc8-ae3498010e71", generation: 1)]
public record UpstreamKeyLinkQuarantined(LinkProposalId LinkProposalId, ReasonId ReasonId, EvidenceId EvidenceId, DateTimeOffset HoldNotBefore);
