// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountLinkStateReducer;

public class when_replaying_erased_personal_data : Specification
{
    ReadModelScenario<AccountLinkState> _scenario;
    AccountLinkState _state;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var subject = AccountSubject.New();
        var link = new LinkId(Guid.NewGuid());
        await _scenario.Given.ForEventSource(subject.Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountNameChanged(new PersonalName(string.Empty)),
            new AccountEmailChanged(new Email(string.Empty)),
            new UpstreamKeyLinkProposed(Guid.NewGuid(), Guid.NewGuid(), "https://github.com", new UpstreamSubject(string.Empty)),
            new UpstreamKeyLinkRecorded(link, Guid.NewGuid(), "https://github.com", new UpstreamLookupDigest(string.Empty), Guid.NewGuid(), DateTimeOffset.UnixEpoch, FactId.New()),
            new UpstreamKeyDisplayIdentifierObserved(link, new Login(string.Empty), DateTimeOffset.UnixEpoch),
            new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountEnablementRecorded(1UL, DateTimeOffset.UnixEpoch, FactId.New()));
        _state = _scenario.InstanceForEventSourceId(subject.Value);
    }

    [Fact] void should_release_an_empty_name() => _state.Name.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_an_empty_email() => _state.Email.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_proposal_to_make_the_personal_field_check_nonvacuous() => _state.Proposals.ShouldContainSingleItem();
    [Fact] void should_release_an_empty_proposed_subject() => _state.Proposals.Values.Single().Subject.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_link_to_make_the_personal_field_check_nonvacuous() => _state.Links.ShouldContainSingleItem();
    [Fact] void should_release_an_empty_link_digest() => _state.Links.Values.Single().KeyHash.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_login_to_make_the_personal_field_check_nonvacuous() => _state.Logins.ShouldContainSingleItem();
    [Fact] void should_release_an_empty_login() => _state.Logins.Values.Single().Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_tombstone_even_after_enablement() => _state.Tombstoned.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
