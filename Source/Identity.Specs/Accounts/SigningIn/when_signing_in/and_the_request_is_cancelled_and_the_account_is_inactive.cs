// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_request_is_cancelled_and_the_account_is_inactive : given.an_account_adapter
{
    CancellationTokenSource _request;
    Exception _error;

    void Establish()
    {
        _request = new();
        _account = _account with { Status = AccountStatus.Disabled };
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<CommandResult>(new CommandResult<Contracts.Accounts.AccountSubject> { Response = _subject }));
        _readModels.GetInstanceById<Account>(Arg.Any<ReadModelKey>(), default).ReturnsForAnyArgs(async _ =>
        {
            await _request.CancelAsync();
            return _account;
        });
    }

    async Task Because() => _error = await Catch.Exception(() => _adapter.SignIn(_verified.Authentication, _verified.Profile, _request.Token));

    void Destroy() => _request.Dispose();

    [Fact] void should_surface_the_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_the_account_as_inactive() => (_error is AccountOperationRefused).ShouldBeFalse();
}
