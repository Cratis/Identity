// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.SigningIn.given;

public class an_account_adapter : a_sign_in_decision
{
    protected IProtocolCommands _commands;
    protected EventSourcedAccountAdapter _adapter;
    protected Account _signedIn;

    void Establish()
    {
        _commands = Substitute.For<IProtocolCommands>();
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>()).Returns(Task.FromResult<CommandResult>(new CommandResult<Contracts.Accounts.AccountSubject> { Response = _subject }));
        _adapter = new(_commands, new(_readModels));
    }

    protected async Task SignIn()
    {
        try
        {
            _signedIn = await _adapter.SignIn(_verified.Authentication!, _verified.Profile!);
        }
        catch (AccountOperationRefused refusal)
        {
            _refusal = refusal.Message;
        }
    }
}
