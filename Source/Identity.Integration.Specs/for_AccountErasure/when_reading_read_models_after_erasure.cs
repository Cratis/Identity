// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Integration.for_AccountErasure.given;

namespace Cratis.Identity.Integration.for_AccountErasure;

/// <summary>
/// After key destruction, the stored account and link state stay unreadable, also after the account projection is
/// replayed. The link state reducer is passive (it never subscribes), so it is read on demand rather than replayed.
/// </summary>
public class when_reading_read_models_after_erasure : IClassFixture<ReadModelsAfterErasure>
{
    readonly ReadModelsAfterErasure _scenario;

    public when_reading_read_models_after_erasure(ReadModelsAfterErasure scenario)
    {
        scenario.Failure?.Throw();
        _scenario = scenario;
    }

    [Fact] void should_read_both_the_existing_and_replayed_account() => _scenario.Accounts.Count.ShouldEqual(2);
    [Fact] void should_read_both_the_existing_and_replayed_link_state() => _scenario.States.Count.ShouldEqual(2);
    [Fact] void should_release_empty_names() => _scenario.Accounts.All(account => account.Name.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_emails() => _scenario.Accounts.All(account => account.Email.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_upstream_subjects() => _scenario.Accounts.All(account => account.GitHubId.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_logins() => _scenario.Accounts.All(account => account.GitHubLogin.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_subjects() => _scenario.Accounts.All(account => account.LinkedUpstreamKeys.Single().IdpSub.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_logins() => _scenario.Accounts.All(account => account.LinkedUpstreamKeys.Single().Login.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_keep_the_terminal_status() => _scenario.Accounts.All(account => account.Status == AccountStatus.Tombstoned).ShouldBeTrue();
    [Fact] void should_keep_the_account_tombstone() => _scenario.Accounts.All(account => account.IsTombstoned).ShouldBeTrue();
    [Fact] void should_release_empty_decision_names() => _scenario.States.All(state => state.Name.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_decision_emails() => _scenario.States.All(state => state.Email.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_proposed_subjects() => _scenario.States.All(state => state.Proposals.Values.Single().Subject.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_digests() => _scenario.States.All(state => state.Links.Values.Single().KeyHash.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_decision_logins() => _scenario.States.All(state => state.Logins.Values.Single().Value.Length == 0).ShouldBeTrue();
    [Fact] void should_keep_the_decision_tombstone() => _scenario.States.All(state => state.Tombstoned).ShouldBeTrue();
    [Fact] void should_never_return_an_active_erased_account() => _scenario.Active.All(account => account is null).ShouldBeTrue();
}
