// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The exact key and proof boundary for one explicit proposal.</summary>
/// <param name="Id">The proposal.</param>
/// <param name="Connection">The connection.</param>
/// <param name="Issuer">The authority.</param>
/// <param name="Subject">The protected subject.</param>
/// <param name="ProposedAt">The server-observed proposal instant.</param>
/// <param name="HoldNotBefore">The hold boundary.</param>
/// <param name="State">The lifecycle state.</param>
public record LinkProposal(LinkProposalId Id, ConnectionId Connection, UpstreamIssuer Issuer, UpstreamSubject Subject, DateTimeOffset ProposedAt, DateTimeOffset HoldNotBefore, string State)
{
    /// <summary>Gets whether the proposal can never progress.</summary>
    internal bool Terminal => State == "confirmed" || State == "rejected";

    /// <summary>Checks the complete immutable upstream tuple.</summary>
    /// <param name="authentication">The adapter proof.</param>
    /// <returns>Whether the proof belongs to this proposal.</returns>
    internal bool Matches(VerifiedAuthentication authentication) => Connection == authentication.ConnectionId && Issuer == authentication.IdpIssuer && Subject == authentication.Subject;

    /// <summary>Checks hold and lifecycle boundaries for confirmation.</summary>
    /// <param name="now">The server clock.</param>
    /// <returns>Whether confirmation is permitted.</returns>
    internal bool Confirmable(DateTimeOffset now) => (State == "pending_proof" || State == "cleared") && now >= HoldNotBefore;

    /// <summary>Checks resolver and post-hold proof timing.</summary>
    /// <param name="now">The server clock.</param>
    /// <param name="authentication">The new-key proof.</param>
    /// <returns>Whether resolver clearance is permitted.</returns>
    internal bool Clearable(DateTimeOffset now, VerifiedAuthentication authentication) => State == "quarantined" && now >= HoldNotBefore && authentication.AuthTime >= HoldNotBefore;
}
