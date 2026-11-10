// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.given;

public class an_account_linking_adapter : a_link_decision
{
    private protected IProtocolCommands _commands;
    private protected AccountLinking _adapter;
    private protected List<LinkCeremony> _attempts = [];
    protected Queue<CommandResult> _results = new();
    protected LinkProposalId _proposal = Guid.NewGuid();
    protected LinkProposalId _proposed;

    void Establish()
    {
        _commands = Substitute.For<IProtocolCommands>();
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _order.Add("execute");
            var ceremony = new LinkCeremony();
            var provider = Substitute.For<IServiceProvider>();
            provider.GetService(typeof(LinkCeremony)).Returns(ceremony);
            call.Arg<Action<IServiceProvider>>()(provider);
            _attempts.Add(ceremony);
            return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : new CommandResult<LinkProposalId> { Response = _proposal });
        });
        _commands.BeforeRetry(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _order.Add("wait");
            return Task.CompletedTask;
        });
        _adapter = new(_commands);
    }

    protected async Task Propose() => _proposed = await _adapter.Propose(_ceremony.Session, _ceremony.Authentication!, _ceremony.Intent);
}
