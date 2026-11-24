// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_a_new_account_signs_in_for_the_first_time : Specification
{
    readonly FactId _operation = FactId.New();
    IEnumerable<object> _events;
    IEnumerable<object> _retried;

    void Because()
    {
        var authentication = new VerifiedAuthentication(GitHubConnection.FixedConnectionId, "https://github.com", "42", DateTimeOffset.UtcNow, _operation);
        var decision = new SignInWithUpstreamKey.Decision(AccountSubject.New(), authentication, new("v1.test-digest"), new("a@example.test", "Name", "login"), null, null, DateTimeOffset.UtcNow);
        var command = new SignInWithUpstreamKey(_operation);
        _events = command.Handle(decision).AsT0.Item1.Select(item => item.Event).ToArray();
        _retried = command.Handle(decision).AsT0.Item1.Select(item => item.Event).ToArray();
    }

    [Fact] void should_register_the_account_and_record_the_sign_in() => _events.OfType<AccountRegistrationRecorded>().Count().ShouldEqual(1);
    [Fact] void should_record_exactly_one_sign_in() => _events.OfType<AccountSignInRecorded>().ShouldContainSingleItem();
    [Fact] void should_record_the_sign_in_after_the_registration() => _events.ToList().FindIndex(@event => @event is AccountSignInRecorded).ShouldBeGreaterThan(_events.ToList().FindIndex(@event => @event is AccountRegistrationRecorded));
    [Fact] void should_keep_the_same_fact_id_when_the_operation_is_retried() => _retried.OfType<AccountSignInRecorded>().Single().FactId.ShouldEqual(_events.OfType<AccountSignInRecorded>().Single().FactId);
}
