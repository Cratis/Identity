// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_catch_up_times_out_in_the_protocol : given.an_account_adapter
{
    void Establish() => _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>()).Returns(Task.FromException<CommandResult>(new ReadModelsNotCaughtUp()));
    Task Because() => SignIn();
    [Fact] void should_map_to_temporary_unavailability() => _refusal.ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_return_a_session_account() => _signedIn.ShouldBeNull();
    [Fact] void should_not_retry_a_progress_failure() => _commands.Received(1).Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>());
}
