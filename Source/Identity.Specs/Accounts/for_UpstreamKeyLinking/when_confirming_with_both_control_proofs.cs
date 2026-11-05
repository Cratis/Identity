// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_confirming_with_both_control_proofs : given.a_link_proposal
{
    LinkId _link;

    Task Establish() => Propose();

    async Task Because()
    {
        _link = await _linking.Confirm(Session(_account), _proposal, _proof);
        await Refresh();
    }

    [Fact] void should_confirm_the_proposal() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("confirmed");
    [Fact] void should_append_the_link() => _states.InstanceForEventSourceId(_account.Value).Links.ContainsKey(_link.Value).ShouldBeTrue();
    [Fact] void should_claim_the_exact_key() => _reservations.Instances.Values.SelectMany(lookup => lookup.Keys).Count().ShouldEqual(2);
}
