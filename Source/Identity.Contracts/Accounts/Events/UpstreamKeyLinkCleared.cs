// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An authorized resolver cleared a quarantine (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ResolutionId">The resolution reference.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="ReasonId">The reason reference.</param>
[EventType("0f4ff8c1-a7e4-48e4-9bd1-65abbe2b465d", generation: 1)]
public record UpstreamKeyLinkCleared(LinkProposalId LinkProposalId, ResolutionId ResolutionId, EvidenceId EvidenceId, ReasonId ReasonId);
