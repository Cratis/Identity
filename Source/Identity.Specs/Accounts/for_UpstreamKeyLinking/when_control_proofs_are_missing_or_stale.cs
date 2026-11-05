// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamKeyLinking;

public class when_control_proofs_are_missing_or_stale : given.a_link_proposal
{
    string _missingAccount;
    string _staleAccount;
    string _staleUpstream;
    string _foreignSession;
    string _differentKey;

    Task Establish() => Propose();

    async Task Because()
    {
        _missingAccount = await Refusal(() => _linking.Confirm(new ClaimsPrincipal(), _proposal, _proof));
        var oldSession = Session(_account);
        _clock.Now += TimeSpan.FromSeconds(301);
        var freshProof = Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(_account));
        _staleAccount = await Refusal(() => _linking.Confirm(oldSession, _proposal, freshProof));
        _staleUpstream = await Refusal(() => _linking.Confirm(Session(_account), _proposal, _proof));
        _foreignSession = await Refusal(() => _linking.Confirm(Session(_account), _proposal, Verified(GitHubIssuer, "candidate-key", FactId.New(), Session(AccountSubject.New()))));
        _differentKey = await Refusal(() => _linking.Confirm(Session(_account), _proposal, Verified(GitHubIssuer, "another-key", FactId.New(), Session(_account))));
        await Refresh();
    }

    [Fact] void should_require_existing_account_control() => _missingAccount.ShouldEqual("existing_account_proof_required");
    [Fact] void should_require_fresh_account_control() => _staleAccount.ShouldEqual("fresh_existing_account_proof_required");
    [Fact] void should_require_fresh_upstream_control() => _staleUpstream.ShouldEqual("fresh_session_bound_upstream_proof_required");
    [Fact] void should_bind_both_proofs_to_the_same_session() => _foreignSession.ShouldEqual("fresh_session_bound_upstream_proof_required");
    [Fact] void should_refuse_proof_of_a_different_key() => _differentKey.ShouldEqual("upstream_proof_mismatch");
    [Fact] void should_grant_nothing() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
}
