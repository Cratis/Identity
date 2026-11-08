// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_reservation_disappears_after_changing_subject : given.a_sign_in_decision
{
    int _positions;

    void Establish() => _releaseProjection = () =>
    {
        if (++_positions == 1)
        {
            _subject = new AccountSubject("22222222-2222-4222-8222-222222222222");
        }
        else
        {
            _readModels.GetInstances<UpstreamKeyLookup>(default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<UpstreamKeyLookup>>([]));
        }
    };

    Task Because() => Decide();

    [Fact] void should_refuse_instead_of_treating_disappearance_as_registration() => _refusal.ShouldEqual("subject_already_reserved");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_wait_for_both_projection_positions() => _positions.ShouldEqual(2);
    [Fact] void should_not_read_an_account_using_the_stale_hint() => _readModels.DidNotReceive().GetInstanceById<Account>(Arg.Any<ReadModelKey>(), default);
}
