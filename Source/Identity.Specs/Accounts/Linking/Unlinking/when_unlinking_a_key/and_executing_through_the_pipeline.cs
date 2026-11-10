// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.Unlinking.when_unlinking_a_key;

public class and_executing_through_the_pipeline : given.a_link_decision
{
    readonly CommandScenario<UnlinkUpstreamKey> _scenario = new();
    readonly LinkId _link = Guid.NewGuid();
    CommandResult _result;

    void Establish()
    {
        ConfigureScenario(_scenario.Services);
        var proof = _ceremony.Authentication!;
        var digest = _digester.Digest(proof.IdpIssuer, proof.Subject);
        LinkId other = Guid.NewGuid();
        _state = _state with
        {
            Links = new Dictionary<Guid, UpstreamKeyLinkRecorded>
            {
                [_link.Value] = new(_link, proof.ConnectionId, proof.IdpIssuer, digest, Guid.NewGuid(), _clock.Now, FactId.New()),
                [other.Value] = new(other, proof.ConnectionId, proof.IdpIssuer, digest, Guid.NewGuid(), _clock.Now, FactId.New())
            }
        };
        _scenario.Given.ForEventSource(_account.Value).ReadModel(_state);
    }

    async Task Because()
    {
        using var scope = ProtocolCommandScope.Begin();
        _result = await _scenario.Execute(new UnlinkUpstreamKey(_link));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_append_the_unlink() => await _scenario.ShouldHaveAppendedEvent<UnlinkUpstreamKey, UpstreamKeyUnlinkRecorded>(_account.Value);
}
