// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking.Clearing.when_clearing_a_link;

public class and_a_resolver_clears_with_fresh_proof : Accounts.given.a_link_proposal
{
    string _timeOnly;
    string _wrongAuthority;
    string _staleProof;
    int _clearances;

    Task Establish() => Propose(true);

    async Task Because()
    {
        _clock.Now += TimeSpan.FromDays(1);
        var fresh = Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_account));
        _timeOnly = await Refusal(() => _linking.Confirm(Session(_account), _proposal, fresh));
        _wrongAuthority = await Refusal(() => _linking.Clear(Session(_account), _account, _proposal, fresh));
        _staleProof = await Refusal(() => _linking.Clear(Session(_account, resolver: true), _account, _proposal, _proof));
        await _linking.Clear(Session(_account, resolver: true), _account, _proposal, fresh);
        await Refresh();
        _clearances = (await _events.EventLog.GetForEventSourceIdAndEventTypes(_account.Value, [_events.EventTypes.GetEventTypeFor(typeof(UpstreamKeyLinkCleared))])).Count;
    }

    [Fact] void should_never_confirm_by_time_alone() => _timeOnly.ShouldEqual("proposal_not_confirmable");
    [Fact] void should_require_the_realm_resolver() => _wrongAuthority.ShouldEqual("authorized_resolver_required");
    [Fact] void should_not_waive_fresh_proof() => _staleProof.ShouldEqual("fresh_session_bound_upstream_proof_required");
    [Fact] void should_record_clearance() => _clearances.ShouldEqual(1);
    [Fact] void should_confirm_in_the_same_commit() => _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("confirmed");
    [Fact] void should_commit_the_link() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(2);
}
