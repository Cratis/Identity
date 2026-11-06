// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountLinkStateReducer;

public class when_registering_and_renaming : Specification
{
    ReadModelScenario<AccountLinkState> _scenario;
    AccountSubject _account;
    LinkId _link;
    AccountLinkState _state;

    void Establish()
    {
        _scenario = new();
        _account = AccountSubject.New();
        _link = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_account.Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new UpstreamKeyLinkRecorded(_link, Guid.NewGuid(), "https://github.com", "v1.digest", Guid.NewGuid(), DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountEmailChanged("before@example.test"),
            new UpstreamKeyDisplayIdentifierObserved(_link, "old-login", DateTimeOffset.UnixEpoch),
            new AccountEmailChanged("after@example.test"),
            new UpstreamKeyDisplayIdentifierObserved(_link, "new-login", DateTimeOffset.UnixEpoch.AddMinutes(1)));
        _state = _scenario.InstanceForEventSourceId(_account.Value);
    }

    [Fact] void should_keep_the_immutable_subject() => _state.Id.ShouldEqual(_account.Value);
    [Fact] void should_keep_one_link() => _state.Links.Count.ShouldEqual(1);
    [Fact] void should_update_display_only() => _state.Logins.Values.Single().Value.ShouldEqual("new-login");
    [Fact] void should_update_email_without_matching_on_it() => _state.Email.Value.ShouldEqual("after@example.test");

    void Destroy() => _scenario.Dispose();
}
