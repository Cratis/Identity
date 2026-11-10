// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Unlinking.when_unlinking_a_key;

public class and_another_key_remains : Accounts.given.a_link_proposal
{
    string _signIn;
    string _relink;

    async Task Establish()
    {
        await Propose();
        await _linking.Confirm(Session(_account), _proposal, _proof);
        await Refresh();
    }

    async Task Because()
    {
        await _linking.Unlink(Session(_account), new(_proposal.Value));
        await Refresh();
        _signIn = await Refusal(() => SignIn(Verified(GitHubIssuer, "candidate-key", FactId.New()), "profile@example.test", "Person", "display"));
        _relink = await Refusal(() => _linking.Confirm(Session(_account), _proposal, _proof));
    }

    [Fact] void should_remove_the_active_link() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
    [Fact] void should_refuse_sign_in_through_the_unlinked_key() => _signIn.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_keep_the_confirmed_flow_terminal() => _relink.ShouldEqual("proposal_not_confirmable");
    [Fact] void should_keep_both_reservations() => _reservations.Instances.Values.Single().Keys.Count().ShouldEqual(2);
}
