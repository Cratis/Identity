// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_both_append_attempts_conflict : given.an_account_adapter
{
    void Establish() => _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] }));
    Task Because() => SignIn();
    [Fact] void should_fail_with_the_defined_reason() => _refusal.ShouldEqual("account_concurrency_conflict");
    [Fact] void should_not_return_a_session_account() => _signedIn.ShouldBeNull();
    [Fact] void should_not_exceed_two_whole_decisions() => _commands.Received(2).Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>());
}
