// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_UniqueUpstreamKey;

public class when_claiming_a_reserved_key : Specification
{
    EventScenario _scenario;
    bool _refused;
    bool _accepted;
    LinkId _link;
    AccountSubject _first;

    void Establish()
    {
        _scenario = new();
        _first = AccountSubject.New();
        _link = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_first.Value).Events(Linked("v1.reserved"));
        await _scenario.Given.ForEventSource(_first.Value).Events(new UpstreamKeyUnlinked(_link, "v1.reserved", "removed", DateTimeOffset.UnixEpoch));
        var refused = await _scenario.When.ForEventSource(AccountSubject.New().Value).Events(Linked("v1.reserved"));
        refused.Errors.ShouldBeEmpty();
        _refused = refused.HasConstraintViolations;
        var accepted = await _scenario.When.ForEventSource(AccountSubject.New().Value).Events(Linked("v1.distinct"));
        accepted.Errors.ShouldBeEmpty();
        _accepted = accepted.IsSuccess;
    }

    [Fact] void should_refuse_reclaim_after_unlink() => _refused.ShouldBeTrue();
    [Fact] void should_allow_a_different_key() => _accepted.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();

    UpstreamKeyLinked Linked(string hash) => new(_link, "github", "https://github.com", hash, Guid.NewGuid(), DateTimeOffset.UnixEpoch);
}
