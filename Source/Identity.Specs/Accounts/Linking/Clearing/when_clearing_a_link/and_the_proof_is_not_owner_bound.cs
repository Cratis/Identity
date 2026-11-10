// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Clearing.when_clearing_a_link;

public class and_the_proof_is_not_owner_bound : Accounts.given.a_link_proposal
{
    Task Establish() => Propose(true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_refuse_missing_or_foreign_session_bindings(bool foreign)
    {
        var proof = Verified(GitHubIssuer, "candidate-key", FactId.New(), foreign ? Session(AccountSubject.New()) : null);
        var refusal = await Refusal(() => _linking.Clear(Session(_account, resolver: true), _account, _proposal, proof));
        await Refresh();
        refusal.ShouldEqual("fresh_session_bound_upstream_proof_required");
        _states.InstanceForEventSourceId(_account.Value).Proposals[_proposal.Value].State.ShouldEqual("quarantined");
        _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_refuse_an_absent_upstream_proof()
    {
        var refusal = await Refusal(() => _linking.Clear(Session(_account, resolver: true), _account, _proposal, null!));
        refusal.ShouldEqual("fresh_session_bound_upstream_proof_required");
    }
}
