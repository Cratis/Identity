// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_AccountErasure.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure;

/// <summary>
/// After key destruction, the stored account and link state stay unreadable, also after the account projection is
/// replayed. The link state reducer is passive (it never subscribes), so it is read on demand rather than replayed.
/// </summary>
/// <param name="host">The host with its own kernel.</param>
[Collection(ErasureReplayCollection.Name)]
public class when_reading_read_models_after_erasure(ErasureReplayHost host) : IAsyncLifetime
{
    const string EmailAddress = "erasure@example.test";
    const string ProposedSubject = "erasure-spec-proposed-subject";
    readonly List<Account> _accounts = [];
    readonly List<AccountLinkState> _states = [];
    readonly List<Account?> _active = [];

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var harness = new ErasureHarness(scope.ServiceProvider);
        var subject = await harness.TombstonedAccount();
        (await harness.Store.EventLog.Append(subject.Value, new AccountEmailChanged(EmailAddress))).IsSuccess.ShouldBeTrue();
        (await harness.Store.EventLog.Append(subject.Value, new UpstreamKeyLinkProposed(Guid.NewGuid(), Guid.NewGuid(), "https://github.com", ProposedSubject))).IsSuccess.ShouldBeTrue();

        // The proposal is not a projection input, so the projection tail is the email change.
        var accountTail = await ReplayWaits.AccountTail(harness.Store);
        await harness.Store.Projections.WaitTillReachesEventSequenceNumber<AccountProjection>(accountTail, ReplayWaits.Bound);
        var reader = new AccountReader(harness.Store.ReadModels);
        await VerifyPersonalDataBeforeErasure(reader, subject);

        var erasure = ErasureId.New();
        (await harness.Requests.Request(new ClaimsPrincipal(), subject, erasure)).ShouldBeTrue();
        (await harness.Coordinator.Run(subject, erasure)).Completed.ShouldBeTrue();
        await ReadAndRejectPlaintext(reader, subject);

        await ReplayWaits.ReplayAccountProjection(harness.Store, await ReplayWaits.AccountTail(harness.Store), host.KernelOutput);
        await ReadAndRejectPlaintext(reader, subject);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    static async Task VerifyPersonalDataBeforeErasure(AccountReader reader, AccountSubject subject)
    {
        var account = await reader.Find(subject);
        var state = await reader.FindLinkState(subject);
        account.ShouldNotBeNull();
        state.ShouldNotBeNull();
        account.Name.Value.ShouldEqual(ErasureHarness.RenamedPersonalName);
        account.Email.Value.ShouldEqual(EmailAddress);
        account.GitHubId.Value.ShouldNotEqual(string.Empty);
        account.GitHubLogin.Value.ShouldEqual("erasure-spec-login");
        account.LinkedUpstreamKeys.ShouldContainSingleItem();
        state.Name.Value.ShouldEqual(ErasureHarness.RenamedPersonalName);
        state.Email.Value.ShouldEqual(EmailAddress);
        state.Proposals.Values.Single().Subject.Value.ShouldEqual(ProposedSubject);
        state.Links.ShouldContainSingleItem();
        state.Logins.Values.Single().Value.ShouldEqual("erasure-spec-login");
    }

    async Task ReadAndRejectPlaintext(AccountReader reader, AccountSubject subject)
    {
        var account = await reader.Find(subject);
        var state = await reader.FindLinkState(subject);
        account.ShouldNotBeNull();
        state.ShouldNotBeNull();

        // Fail here if key destruction leaves the stored personal data readable.
        account.Name.Value.ShouldNotEqual(ErasureHarness.RenamedPersonalName);
        account.Email.Value.ShouldNotEqual(EmailAddress);
        account.GitHubLogin.Value.ShouldNotEqual("erasure-spec-login");
        state.Name.Value.ShouldNotEqual(ErasureHarness.RenamedPersonalName);
        state.Email.Value.ShouldNotEqual(EmailAddress);
        state.Proposals.Values.Single().Subject.Value.ShouldNotEqual(ProposedSubject);
        state.Logins.Values.Single().Value.ShouldNotEqual("erasure-spec-login");
        _accounts.Add(account);
        _states.Add(state);
        _active.Add(await reader.FindActive(subject));
    }

    [Fact] void should_read_both_the_existing_and_replayed_account() => _accounts.Count.ShouldEqual(2);
    [Fact] void should_read_both_the_existing_and_replayed_link_state() => _states.Count.ShouldEqual(2);
    [Fact] void should_release_empty_names() => _accounts.TrueForAll(account => account.Name.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_emails() => _accounts.TrueForAll(account => account.Email.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_upstream_subjects() => _accounts.TrueForAll(account => account.GitHubId.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_logins() => _accounts.TrueForAll(account => account.GitHubLogin.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_subjects() => _accounts.TrueForAll(account => account.LinkedUpstreamKeys.Single().IdpSub.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_logins() => _accounts.TrueForAll(account => account.LinkedUpstreamKeys.Single().Login.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_keep_the_terminal_status() => _accounts.TrueForAll(account => account.Status == AccountStatus.Tombstoned).ShouldBeTrue();
    [Fact] void should_keep_the_account_tombstone() => _accounts.TrueForAll(account => account.IsTombstoned).ShouldBeTrue();
    [Fact] void should_release_empty_decision_names() => _states.TrueForAll(state => state.Name.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_decision_emails() => _states.TrueForAll(state => state.Email.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_proposed_subjects() => _states.TrueForAll(state => state.Proposals.Values.Single().Subject.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_link_digests() => _states.TrueForAll(state => state.Links.Values.Single().KeyHash.Value.Length == 0).ShouldBeTrue();
    [Fact] void should_release_empty_decision_logins() => _states.TrueForAll(state => state.Logins.Values.Single().Value.Length == 0).ShouldBeTrue();
    [Fact] void should_keep_the_decision_tombstone() => _states.TrueForAll(state => state.Tombstoned).ShouldBeTrue();
    [Fact] void should_never_return_an_active_erased_account() => _active.TrueForAll(account => account is null).ShouldBeTrue();
}
