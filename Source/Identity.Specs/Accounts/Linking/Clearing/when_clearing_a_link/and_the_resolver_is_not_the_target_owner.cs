// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Clearing.when_clearing_a_link;

public class and_the_resolver_is_not_the_target_owner : Accounts.given.a_link_proposal
{
    string _refusal;

    Task Establish() => Propose(true);

    async Task Because()
    {
        var otherSession = Session(AccountSubject.New(), resolver: true);
        var proof = Verified(GitHubIssuer, "candidate-key", FactId.New(), otherSession);
        _refusal = await Refusal(() => _linking.Clear(otherSession, _account, _proposal, proof));
        await Refresh();
    }

    [Fact] void should_require_the_target_owners_account_proof() => _refusal.ShouldEqual("target_account_proof_required");
    [Fact] void should_leave_the_proposal_in_quarantine() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("quarantined");
    [Fact] void should_not_grant_the_candidate_key() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
}
