// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_fresh_retry_observes_an_unlink : given.an_account_adapter
{
    void Establish() => _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>()).Returns(
        Task.FromResult(new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] }),
        Task.FromResult(new CommandResult { ValidationResults = [ValidationResult.Error("upstream_key_reserved_but_inactive")] }));
    Task Because() => SignIn();
    [Fact] void should_refuse_instead_of_replaying_stale_events() => _refusal.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_not_return_a_session_account() => _signedIn.ShouldBeNull();
    [Fact] void should_decide_at_most_twice() => _commands.Received(2).Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>());
}
