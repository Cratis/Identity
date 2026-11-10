// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Rejecting.when_rejecting_a_link;

public class and_the_proposal_is_terminal : given.a_link_decision
{
    readonly LinkProposalId _proposal = Guid.NewGuid();
    string _refusal;

    void Establish()
    {
        var proof = _ceremony.Authentication!;
        _state = _state with { Proposals = new Dictionary<Guid, LinkProposal> { [_proposal.Value] = new(_proposal, proof.ConnectionId, proof.IdpIssuer, proof.Subject, _clock.Now, _clock.Now, "confirmed") } };
    }

    void Because()
    {
        var result = new RejectLink(_proposal).Handle(new(_account, _state));
        if (result.TryGetError(out var refusal))
        {
            _refusal = refusal.Message;
        }
    }

    [Fact] void should_refuse_a_second_terminal_transition() => _refusal.ShouldEqual("terminal_proposal");
}
