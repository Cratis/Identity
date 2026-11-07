// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_reservation_changes_subject_during_catch_up : given.a_sign_in_decision
{
    void Establish() => _releaseProjection = () => _subject = new AccountSubject("22222222-2222-4222-8222-222222222222");

    Task Because() => Decide();

    [Fact] void should_require_a_fresh_decision() => _refusal.ShouldEqual("subject_already_reserved");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_not_read_an_account_using_the_stale_hint() => _readModels.DidNotReceive().GetInstanceById<Account>(Arg.Any<ReadModelKey>(), default);
}
