// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Cratis.Monads;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking.Proposing;

/// <summary>
/// Proposes an explicitly requested link using scope-local dual proof.
/// </summary>
[Command, ProtocolCommand, ExcludeFromDiscovery, ProtectedDecision]
internal record ProposeLink
{
    public async Task<Result<LinkProposalDecision, ValidationResult>> Provide(ILinkCeremony ceremony, IDecisionReads decisions, IReadModels readModels, UpstreamLookups lookups, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
    {
        if (ceremony.Intent.EmailOnlyProof)
        {
            return ValidationResult.Error("email_is_not_identity_proof");
        }

        var session = LinkDecisions.RequireSession(ceremony.Session, time);
        if (session.TryGetError(out var refusal))
        {
            return refusal;
        }

        if (!ceremony.Intent.Explicit)
        {
            return ValidationResult.Error("explicit_dual_proof_required");
        }

        var existing = session.AsT0;
        if (LinkDecisions.RequireProof(ceremony.Authentication, existing.SessionId, time.GetUtcNow()) is { } proofRefusal)
        {
            return proofRefusal;
        }

        var authentication = ceremony.Authentication!;
        var reserved = await lookups.Find(digester.Digest(authentication.IdpIssuer, authentication.Subject)) is not null;
        await decisions.Get<AccountGuard>(new ReadModelKey(existing.Account.Value));
        var state = await readModels.GetInstanceById<AccountLinkState>(new ReadModelKey(existing.Account.Value)) ?? new(existing.Account.Value);
        return new LinkProposalDecision(existing.Account, state, authentication, ceremony.Intent, reserved, options.Value.Accounts.LinkHoldMinimum, time.GetUtcNow());
    }

    public Result<(IEnumerable<EventForEventSourceId>, LinkProposalId), ValidationResult> Handle(LinkProposalDecision decision)
    {
        if (LinkDecisions.RequireActive(decision.State) is { } refusal)
        {
            return refusal;
        }

        var authentication = decision.Authentication;
        if (decision.State.Proposals.Values.Any(proposal => proposal.Matches(authentication) && proposal.State != "rejected"))
        {
            return ValidationResult.Error("proposal_already_exists");
        }

        LinkProposalId id = Guid.NewGuid();
        var events = new List<object> { new UpstreamKeyLinkProposed(id, authentication.ConnectionId, authentication.IdpIssuer, authentication.Subject) };
        if (decision.Reserved || decision.Intent.SoftDisabledConnection)
        {
            events.Add(new UpstreamKeyLinkQuarantined(id, decision.Reserved ? "upstream_key_already_linked" : "soft_disabled_connection", Guid.NewGuid(), decision.At + decision.Hold));
        }

        return (events.Select(@event => new EventForEventSourceId(decision.Account.Value, @event) { Occurred = decision.At }).ToArray(), id);
    }
}

/// <summary>
/// Guarded account state and trusted observations for the proposal decision.
/// </summary>
internal record LinkProposalDecision(AccountSubject Account, AccountLinkState State, VerifiedAuthentication Authentication, LinkIntent Intent, bool Reserved, TimeSpan Hold, DateTimeOffset At);
