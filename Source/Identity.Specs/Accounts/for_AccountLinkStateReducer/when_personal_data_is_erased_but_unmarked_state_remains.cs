// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountLinkStateReducer;

public class when_personal_data_is_erased_but_unmarked_state_remains : Specification
{
    static readonly DateTimeOffset _linkedAt = DateTimeOffset.UnixEpoch.AddDays(1);

    ReadModelScenario<AccountLinkState> _scenario;
    AccountLinkState _state;
    Guid _link;
    Guid _connection;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var subject = AccountSubject.New();
        _link = Guid.NewGuid();
        _connection = Guid.NewGuid();
        await _scenario.Given.ForEventSource(subject.Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountNameChanged(new PersonalName(string.Empty)),
            new AccountEmailChanged(new Email(string.Empty)),
            new UpstreamKeyLinkRecorded(new LinkId(_link), _connection, "https://github.com", new UpstreamLookupDigest(string.Empty), Guid.NewGuid(), _linkedAt, FactId.New()),
            new UpstreamKeyDisplayIdentifierObserved(new LinkId(_link), new Login(string.Empty), DateTimeOffset.UnixEpoch));
        _state = _scenario.InstanceForEventSourceId(subject.Value);
    }

    [Fact] void should_keep_the_account_registered() => _state.Registered.ShouldBeTrue();
    [Fact] void should_not_mark_the_account_tombstoned() => _state.Tombstoned.ShouldBeFalse();
    [Fact] void should_not_mark_the_account_disabled() => _state.Disabled.ShouldBeFalse();
    [Fact] void should_keep_the_link_under_its_id() => _state.Links.Keys.Single().ShouldEqual(_link);
    [Fact] void should_keep_the_link_issuer() => _state.Links[_link].IdpIssuer.Value.ShouldEqual("https://github.com");
    [Fact] void should_keep_the_link_time() => _state.Links[_link].LinkedAt.ShouldEqual(_linkedAt);
    [Fact] void should_release_an_empty_link_digest() => _state.Links[_link].KeyHash.Value.ShouldEqual(string.Empty);

    void Destroy() => _scenario.Dispose();
}
