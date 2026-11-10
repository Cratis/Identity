// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Proposing.when_proposing_a_link;

public class and_the_key_is_already_linked : Accounts.given.a_contract_vector
{
    AccountSubject _requester;
    LinkProposalId _proposal;

    async Task Establish()
    {
        await Load("already-linked-key");
        _requester = new(Text(_when.GetProperty("target_account"), "subject"));
    }

    async Task Because()
    {
        var proof = VectorProof(FactId.New(), Session(_requester));
        _proposal = await _linking.Propose(Session(_requester), proof, new(true));
        await Refresh();
    }

    [Fact] void should_quarantine_the_conflict() => _states.InstanceForEventSourceId(_requester.Value).Proposals[_proposal.Value].State.ShouldEqual(Text(_then, "proposal_state"));
    [Fact] void should_not_grant_a_link() => _states.InstanceForEventSourceId(_requester.Value).Links.Count.ShouldEqual(1);
    [Fact]
    void should_leave_the_original_binding_unchanged()
    {
        var binding = _given.GetProperty("accounts")[0];
        var link = _states.InstanceForEventSourceId(Text(binding, "subject")).Links.Values.Single();
        link.KeyHash.ShouldEqual(_digester.Digest(new(Text(binding.GetProperty("linked_keys")[0], "idp")), new(Text(binding.GetProperty("linked_keys")[0], "idp_sub"))));
    }
}
