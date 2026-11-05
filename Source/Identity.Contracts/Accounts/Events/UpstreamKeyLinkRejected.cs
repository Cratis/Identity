// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A proposal was terminally rejected (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ReasonId">The reason reference.</param>
[EventType("81cf15b3-1e52-441d-b55c-0b89853f8861", generation: 1)]
public record UpstreamKeyLinkRejected(LinkProposalId LinkProposalId, ReasonId ReasonId);
