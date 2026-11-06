// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_AccountProjection;

public class when_a_link_is_removed : Specification
{
    ReadModelScenario<Account> _scenario;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var link = new LinkId(Guid.NewGuid());
        await _scenario.Given.ForEventSource(AccountSubject.New().Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new UpstreamKeyLinkRecorded(link, GitHubConnection.FixedConnectionId, "https://github.com", "digest", new(Guid.NewGuid()), DateTimeOffset.UnixEpoch, FactId.New()),
            new UpstreamKeySubjectRecorded(link, "https://github.com", "12345"),
            new UpstreamKeyUnlinkRecorded(link, "digest", "spec_reason", DateTimeOffset.UnixEpoch, FactId.New()));
    }

    [Fact] void should_remove_the_raw_child() => _scenario.Instance.LinkedUpstreamKeys.ShouldBeEmpty();

    void Destroy() => _scenario.Dispose();
}
