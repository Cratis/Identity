// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.Proposing.when_proposing_a_link;

public class and_executing_through_the_pipeline : given.a_link_decision
{
    readonly CommandScenario<ProposeLink> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        ConfigureScenario(_scenario.Services);
        _scenario.Given.ForEventSource(_account.Value).ReadModel(_state);
    }

    async Task Because()
    {
        using var scope = ProtocolCommandScope.Begin();
        _result = await _scenario.Execute(new ProposeLink());
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_append_the_proposal() => await _scenario.ShouldHaveAppendedEvent<ProposeLink, UpstreamKeyLinkProposed>(_account.Value, proposed => proposed.Subject == _ceremony.Authentication!.Subject);
}
