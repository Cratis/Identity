// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountProjection;

public class when_only_some_personal_data_is_erased : Specification
{
    static readonly DateTimeOffset _linkedAt = DateTimeOffset.UnixEpoch.AddDays(1);

    ReadModelScenario<Account> _scenario;
    Account _account;
    LinkId _link;
    Guid _connection;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var subject = AccountSubject.New();
        _link = new LinkId(Guid.NewGuid());
        _connection = Guid.NewGuid();
        await _scenario.Given.ForEventSource(subject.Value).Events(
            new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
            new AccountNameChanged(new PersonalName(string.Empty)),
            new AccountEmailChanged(new Email("readable@example.test")),
            new UpstreamKeyLinkRecorded(_link, _connection, "https://github.com", new UpstreamLookupDigest("digest"), Guid.NewGuid(), _linkedAt, FactId.New()),
            new UpstreamKeySubjectRecorded(_link, "https://github.com", new UpstreamSubject(string.Empty)),
            new UpstreamKeyDisplayIdentifierObserved(_link, new Login("readable-login"), DateTimeOffset.UnixEpoch));
        _account = _scenario.InstanceForEventSourceId(subject.Value);
    }

    [Fact] void should_release_the_erased_name_as_empty() => _account.Name.Value.ShouldEqual(string.Empty);
    [Fact] void should_release_the_erased_upstream_subject_as_empty() => _account.LinkedUpstreamKeys.Single().IdpSub.Value.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_readable_email() => _account.Email.Value.ShouldEqual("readable@example.test");
    [Fact] void should_keep_the_readable_login() => _account.LinkedUpstreamKeys.Single().Login.Value.ShouldEqual("readable-login");
    [Fact] void should_keep_the_account_active() => _account.Status.ShouldEqual(AccountStatus.Active);
    [Fact] void should_not_mark_the_account_tombstoned() => _account.IsTombstoned.ShouldBeFalse();
    [Fact] void should_keep_the_link_identity() => _account.LinkedUpstreamKeys.Single().LinkId.ShouldEqual(_link);
    [Fact] void should_keep_the_link_issuer() => _account.LinkedUpstreamKeys.Single().IdpIssuer.Value.ShouldEqual("https://github.com");
    [Fact] void should_keep_the_link_time() => _account.LinkedUpstreamKeys.Single().LinkedAt.ShouldEqual(_linkedAt);

    void Destroy() => _scenario.Dispose();
}
