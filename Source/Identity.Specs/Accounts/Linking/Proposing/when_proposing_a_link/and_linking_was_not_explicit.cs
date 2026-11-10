// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Linking.Proposing.when_proposing_a_link;

public class and_linking_was_not_explicit : Accounts.given.a_link_proposal
{
    string _refusal;

    async Task Because()
    {
        _refusal = await Refusal(() => _linking.Propose(Session(_account), _proof, new(false)));
        await Refresh();
    }

    [Fact] void should_refuse_an_implicit_link() => _refusal.ShouldEqual("explicit_dual_proof_required");
    [Fact] void should_not_make_a_proposal() => _states.InstanceForEventSourceId(_account.Value).Proposals.ShouldBeEmpty();
}
