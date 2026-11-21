// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinkState;

public class when_finding_a_proposal : Specification
{
    LinkProposal _proposal;
    AccountLinkState _state;
    LinkProposal _found;
    ValidationResult _refusal;

    void Establish()
    {
        var id = Guid.NewGuid();
        _proposal = new(id, new ConnectionId(Guid.NewGuid()), "https://github.com", "42", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "pending_proof");
        _state = new("a") { Proposals = new Dictionary<Guid, LinkProposal> { [id] = _proposal } };
    }

    void Because()
    {
        _found = AccountLinkState.FindProposal(_state, _proposal.Id).AsT0;
        AccountLinkState.FindProposal(_state, Guid.NewGuid()).TryGetError(out _refusal);
    }

    [Fact] void should_return_the_known_proposal() => _found.ShouldEqual(_proposal);
    [Fact] void should_refuse_an_unknown_proposal() => _refusal.Message.ShouldEqual("proposal_not_found");
}
