// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountProjection;

public class when_replaying_erased_personal_data : Specification
{
    ReadModelScenario<Account> _scenario;
    Account _account;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var subject = AccountSubject.New();
        var link = new LinkId(Guid.NewGuid());
        await _scenario.Given.ForEventSource(subject.Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountNameChanged(new PersonalName(string.Empty)),
            new AccountEmailChanged(new Email(string.Empty)),
            new UpstreamKeyLinkRecorded(link, Guid.NewGuid(), "https://github.com", new UpstreamLookupDigest(string.Empty), Guid.NewGuid(), DateTimeOffset.UnixEpoch, FactId.New()),
            new UpstreamKeySubjectRecorded(link, "https://github.com", new UpstreamSubject(string.Empty)),
            new UpstreamKeyDisplayIdentifierObserved(link, new Login(string.Empty), DateTimeOffset.UnixEpoch),
            new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountEnablementRecorded(1UL, DateTimeOffset.UnixEpoch, FactId.New()));
        _account = _scenario.InstanceForEventSourceId(subject.Value);
    }

    [Fact] void should_release_an_empty_name() => _account.Name.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_an_empty_email() => _account.Email.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_an_empty_upstream_subject() => _account.GitHubId.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_an_empty_login() => _account.GitHubLogin.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_link_to_make_the_personal_field_checks_nonvacuous() => _account.LinkedUpstreamKeys.ShouldContainSingleItem();
    [Fact] void should_release_an_empty_link_subject() => _account.LinkedUpstreamKeys.Single().IdpSub.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_an_empty_link_login() => _account.LinkedUpstreamKeys.Single().Login.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_terminal_status_even_after_enablement() => _account.Status.ShouldEqual(AccountStatus.Tombstoned);
    [Fact] void should_keep_the_tombstone() => _account.IsTombstoned.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
