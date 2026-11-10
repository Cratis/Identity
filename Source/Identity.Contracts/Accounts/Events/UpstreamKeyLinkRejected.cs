// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>A proposal was terminally rejected (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ReasonId">The reason reference.</param>
[EventType]
public record UpstreamKeyLinkRejected(LinkProposalId LinkProposalId, ReasonId ReasonId);
