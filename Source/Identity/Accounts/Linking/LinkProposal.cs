// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The exact key and proof boundary for one explicit proposal.</summary>
/// <param name="Id">The proposal.</param>
/// <param name="Connection">The connection.</param>
/// <param name="Issuer">The authority.</param>
/// <param name="Subject">The protected subject.</param>
/// <param name="ProposedAt">The server-observed proposal instant.</param>
/// <param name="HoldNotBefore">The hold boundary.</param>
/// <param name="State">The lifecycle state.</param>
public record LinkProposal(LinkProposalId Id, ConnectionId Connection, UpstreamIssuer Issuer, UpstreamSubject Subject, DateTimeOffset ProposedAt, DateTimeOffset HoldNotBefore, string State);
