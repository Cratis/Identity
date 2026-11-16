// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An authorized resolver cleared a quarantine (Internal).</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ResolutionId">The resolution reference.</param>
/// <param name="EvidenceId">The evidence reference.</param>
/// <param name="ReasonId">The reason reference.</param>
[EventType]
public record UpstreamKeyLinkCleared(LinkProposalId LinkProposalId, ResolutionId ResolutionId, EvidenceId EvidenceId, ReasonId ReasonId);
