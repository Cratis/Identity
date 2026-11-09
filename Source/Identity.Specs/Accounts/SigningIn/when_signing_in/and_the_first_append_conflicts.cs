// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_first_append_conflicts : given.an_account_adapter
{
    void Establish() => _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>()).Returns(
        Task.FromResult(new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] }),
        Task.FromResult<CommandResult>(new CommandResult<AccountSubject> { Response = _subject }));

    Task Because() => SignIn();

    [Fact] void should_use_the_fresh_decision_result() => _signedIn.Id.ShouldEqual(_subject);
    [Fact] void should_retry_the_whole_command_once() => _commands.Received(2).Execute(Arg.Is<object>(command => command is SignInWithUpstreamKey), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>());
    [Fact] void should_wait_for_projection_progress_before_retrying() => _commands.Received(1).BeforeRetry(Arg.Any<CancellationToken>());
}
