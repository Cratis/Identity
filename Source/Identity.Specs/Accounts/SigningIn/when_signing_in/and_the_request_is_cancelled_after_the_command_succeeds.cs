// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_request_is_cancelled_after_the_command_succeeds : given.an_account_adapter
{
    CancellationTokenSource _request;
    Exception _error;

    void Establish()
    {
        _request = new();
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await _request.CancelAsync();
            return (CommandResult)new CommandResult<Contracts.Accounts.AccountSubject> { Response = _subject };
        });
    }

    async Task Because() => _error = await Catch.Exception(() => _adapter.SignIn(_verified.Authentication, _verified.Profile, _request.Token));

    void Destroy() => _request.Dispose();

    [Fact] void should_surface_the_cancellation_instead_of_a_session_account() => (_error is OperationCanceledException).ShouldBeTrue();
}
