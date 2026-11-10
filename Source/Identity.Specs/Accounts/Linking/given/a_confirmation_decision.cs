// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Accounts.Linking.Clearing;
using Cratis.Identity.Accounts.Linking.Confirming;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking.given;

public class a_confirmation_decision : a_link_decision
{
    protected LinkProposalId _proposalId;
    private protected LinkProposal _proposal;
    private protected LinkConfirmationDecision _decision;

    void Establish()
    {
        ((ClaimsIdentity)_ceremony.Session.Identity).AddClaim(new("permission", "identity:resolve-links"));
        _proposalId = Guid.NewGuid();
        var proof = _ceremony.Authentication;
        _proposal = new(_proposalId, proof.ConnectionId, proof.IdpIssuer, proof.Subject, _clock.Now - TimeSpan.FromMinutes(1), DateTimeOffset.MinValue, "pending_proof");
        _state = _state with { Proposals = new Dictionary<Guid, LinkProposal> { [_proposalId.Value] = _proposal } };
        _decision = new(_account, _state, proof, _digester.Digest(proof.IdpIssuer, proof.Subject), false, TimeSpan.Zero, _clock.Now);
    }

    private protected Task<Result<LinkConfirmationDecision, ValidationResult>> Provide(bool clearing) => clearing
        ? new ClearLinkQuarantine(_account, _proposalId).Provide(_ceremony, _decisions, new AccountReader(_readModels), _lookups, _digester, _options, _clock)
        : new ConfirmLink(_proposalId).Provide(_ceremony, _decisions, new AccountReader(_readModels), _lookups, _digester, _options, _clock);

    private protected Result<(IEnumerable<EventForEventSourceId>, LinkConfirmationOutcome), ValidationResult> Handle(bool clearing) => clearing
        ? new ClearLinkQuarantine(_account, _proposalId).Handle(_decision)
        : new ConfirmLink(_proposalId).Handle(_decision);

    private protected void Observe(VerifiedAuthentication? authentication)
    {
        var session = _ceremony.Session;
        _ceremony = new();
        _ceremony.Set(session, authentication, new(false), CancellationToken.None);
    }

    protected void SetState(string state) => SetProposal(_proposal with { State = state });

    private protected void SetProposal(LinkProposal proposal)
    {
        _proposal = proposal;
        _state = _state with { Proposals = new Dictionary<Guid, LinkProposal> { [_proposalId.Value] = proposal } };
        _decision = _decision with { State = _state };
    }

    private protected static void Refused<T>(Result<T, ValidationResult> result, string reason)
    {
        result.TryGetError(out var refusal).ShouldBeTrue();
        refusal.Message.ShouldEqual(reason);
    }
}
