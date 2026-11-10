// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_pipeline_captures_a_catch_up_timeout : given.an_account_adapter
{
    void Establish() => _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommandResult { ExceptionMessages = ["read_models_not_caught_up"] }));
    Task Because() => SignIn();
    [Fact] void should_map_to_temporary_unavailability() => _refusal.ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_return_a_session_account() => _signedIn.ShouldBeNull();
}
