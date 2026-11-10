// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Identity.Contracts.Accounts.Events;

/// <summary>An explicit link intent was accepted, without access (Internal). See the #26 identity-events-v1 catalogue.</summary>
/// <param name="LinkProposalId">The proposal.</param>
/// <param name="ConnectionId">The connection.</param>
/// <param name="IdpIssuer">The exact upstream authority.</param>
/// <param name="IdpSub">The protected raw subject.</param>
[EventType]
public record UpstreamKeyLinkProposed(LinkProposalId LinkProposalId, ConnectionId ConnectionId, UpstreamIssuer IdpIssuer, UpstreamSubject IdpSub);
