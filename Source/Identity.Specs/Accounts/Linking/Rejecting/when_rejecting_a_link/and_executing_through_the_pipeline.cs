// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.Rejecting.when_rejecting_a_link;

public class and_executing_through_the_pipeline : given.a_link_decision
{
    readonly CommandScenario<RejectLink> _scenario = new();
    readonly LinkProposalId _proposal = Guid.NewGuid();
    CommandResult _result;

    void Establish()
    {
        ConfigureScenario(_scenario.Services);
        var proof = _ceremony.Authentication!;
        _state = _state with { Proposals = new Dictionary<Guid, LinkProposal> { [_proposal.Value] = new(_proposal, proof.ConnectionId, proof.IdpIssuer, proof.Subject, _clock.Now, _clock.Now, "pending_proof") } };
        _scenario.Given.ForEventSource(_account.Value).ReadModel(_state);
    }

    async Task Because()
    {
        using var scope = ProtocolCommandScope.Begin();
        _result = await _scenario.Execute(new RejectLink(_proposal));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_append_the_rejection() => await _scenario.ShouldHaveAppendedEvent<RejectLink, UpstreamKeyLinkRejected>(_account.Value);
}
