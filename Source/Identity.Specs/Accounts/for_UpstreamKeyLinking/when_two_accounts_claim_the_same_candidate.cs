// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_two_accounts_claim_the_same_candidate : given.a_link_proposal
{
    AccountSubject _other;
    LinkProposalId _otherProposal;
    string?[] _results;

    async Task Establish()
    {
        _other = AccountSubject.New();
        await Seed(_other, GitHubIssuer, "other-existing-key");
        await Refresh();
        await Propose();
        _otherProposal = await _linking.Propose(Session(_other), Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_other)), new(true));
        await Refresh();
    }

    async Task Because()
    {
        _results = await Task.WhenAll(
            Refusal(() => _linking.Confirm(Session(_account), _proposal, _proof)),
            Refusal(() => _linking.Confirm(Session(_other), _otherProposal, Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_other)))));
        await Refresh();
    }

    [Fact] void should_have_exactly_one_winner() => _results.Count(result => result is null).ShouldEqual(1);
    [Fact] void should_report_one_conflict() => _results.Count(result => result == "upstream_key_already_reserved").ShouldEqual(1);
    [Fact] void should_quarantine_the_loser() => _states.Instances.Values.SelectMany(state => state.Proposals.Values).Count(proposal => proposal.State == "quarantined").ShouldEqual(1);
    [Fact] void should_commit_only_one_candidate_link() => _states.Instances.Values.Sum(state => state.Links.Count).ShouldEqual(3);
}
