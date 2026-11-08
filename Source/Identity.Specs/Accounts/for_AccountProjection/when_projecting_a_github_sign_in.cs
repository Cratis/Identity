// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_AccountProjection;

public class when_projecting_a_github_sign_in : Specification
{
    ReadModelScenario<Account> _scenario;
    AccountSubject _subject;
    LinkId _link;

    void Establish()
    {
        _scenario = new();
        _subject = AccountSubject.New();
        _link = new(Guid.NewGuid());
    }

    async Task Because() => await _scenario.Given.ForEventSource(_subject.Value).Events(
        new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
        new AccountNameChanged("Person"),
        new AccountEmailChanged("profile@example.test"),
        new UpstreamKeyLinkRecorded(_link, GitHubConnection.FixedConnectionId, "https://github.com", "digest", new(Guid.NewGuid()), DateTimeOffset.UnixEpoch, FactId.New()),
        new UpstreamKeySubjectRecorded(_link, "https://github.com", "12345"),
        new UpstreamKeyDisplayIdentifierObserved(_link, "display", DateTimeOffset.UnixEpoch));

    [Fact] void should_use_the_canonical_account_subject() => _scenario.Instance.Id.ShouldEqual(_subject);
    [Fact] void should_project_the_immutable_github_id() => _scenario.Instance.GitHubId.Value.ShouldEqual("12345");
    [Fact] void should_project_the_display_login() => _scenario.Instance.GitHubLogin.Value.ShouldEqual("display");
    [Fact] void should_project_the_profile_name() => _scenario.Instance.Name.Value.ShouldEqual("Person");
    [Fact] void should_project_the_profile_email() => _scenario.Instance.Email.Value.ShouldEqual("profile@example.test");
    [Fact] void should_be_active() => _scenario.Instance.Status.ShouldEqual(AccountStatus.Active);
    [Fact] void should_keep_the_raw_link_subject_inside_the_account_boundary() => _scenario.Instance.LinkedUpstreamKeys.Single().IdpSub.Value.ShouldEqual("12345");

    void Destroy() => _scenario.Dispose();
}
