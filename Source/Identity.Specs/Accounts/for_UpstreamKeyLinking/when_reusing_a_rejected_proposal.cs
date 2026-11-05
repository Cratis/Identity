// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_reusing_a_rejected_proposal : given.a_link_proposal
{
    string _confirmation;
    string _clearance;

    Task Establish() => Propose(true);

    async Task Because()
    {
        await _linking.Reject(Session(_account), _proposal);
        await Refresh();
        _confirmation = await Refusal(() => _linking.Confirm(Session(_account), _proposal, _proof));
        _clearance = await Refusal(() => _linking.Clear(Session(_account, resolver: true), _account, _proposal, _proof));
    }

    [Fact] void should_remain_rejected() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("rejected");
    [Fact] void should_not_confirm() => _confirmation.ShouldEqual("proposal_not_confirmable");
    [Fact] void should_not_clear() => _clearance.ShouldEqual("quarantine_not_clearable");
    [Fact] void should_grant_no_link() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
}
