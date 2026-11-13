// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_AccountErasure.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure;

[Collection(IdentityCollection.Name)]
public class when_reading_read_models_after_erasure(IdentityHost host) : IAsyncLifetime
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
        var events = await harness.Store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject.Value);
        var tail = events[^1].Context.SequenceNumber;
        await harness.Store.Projections.WaitTillReachesEventSequenceNumber<AccountProjection>(tail, TimeSpan.FromSeconds(30));
        await harness.Store.Reducers.WaitTillReachesEventSequenceNumber<AccountLinkStateReducer>(tail, TimeSpan.FromSeconds(30));
        var reader = new AccountReader(harness.Store.ReadModels);
        await VerifyPersonalDataBeforeErasure(reader, subject);

        var erasure = ErasureId.New();
        (await harness.Requests.Request(new ClaimsPrincipal(), subject, erasure)).ShouldBeTrue();
        (await harness.Coordinator.Run(subject, erasure)).Completed.ShouldBeTrue();
        await ReadAndRejectPlaintext(reader, subject);

        var projectionReplay = await harness.Store.Projections.Replay<AccountProjection>();
        projectionReplay.ShouldNotEqual(JobId.NotSet);
        await harness.Store.Jobs.WaitTillJobProgressCompleted(projectionReplay, TimeSpan.FromSeconds(60));
        var reducerReplay = await harness.Store.Reducers.Replay<AccountLinkStateReducer>();
        reducerReplay.ShouldNotEqual(JobId.NotSet);
        await harness.Store.Jobs.WaitTillJobProgressCompleted(reducerReplay, TimeSpan.FromSeconds(60));
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

        // Fail before replay if key destruction leaves the stored personal data readable.
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
