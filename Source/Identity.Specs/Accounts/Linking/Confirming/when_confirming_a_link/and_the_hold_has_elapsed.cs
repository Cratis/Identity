// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_the_hold_has_elapsed : Accounts.given.a_link_proposal
{
    string _staleProof;

    async Task Establish()
    {
        _options.Accounts.LinkHoldMinimum = TimeSpan.FromSeconds(30);
        await Propose();
    }

    async Task Because()
    {
        _clock.Now += TimeSpan.FromSeconds(30);
        _staleProof = await Refusal(() => _linking.Confirm(Session(_account), _proposal, _proof));
        await _linking.Confirm(Session(_account), _proposal, Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_account)));
        await Refresh();
    }

    [Fact] void should_require_proof_at_or_after_the_boundary() => _staleProof.ShouldEqual("fresh_post_hold_proof_required");
    [Fact] void should_allow_fresh_proof_at_the_exact_boundary() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("confirmed");
    [Fact] void should_commit_one_additional_link() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(2);
}
