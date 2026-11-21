// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using System.Security.Claims;
using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure.given;

/// <summary>
/// Captures the erasure and replay scenario once on its own host for all read-model assertions.
/// </summary>
public class ReadModelsAfterErasure : IAsyncLifetime
{
    const string EmailAddress = "erasure@example.test";
    const string ProposedSubject = "erasure-spec-proposed-subject";
    readonly ErasureReplayHost _host = new();
    readonly List<Account> _accounts = [];
    readonly List<AccountLinkState> _states = [];
    readonly List<Account?> _active = [];

    internal Account? BeforeAccount { get; private set; }
    internal AccountLinkState? BeforeState { get; private set; }
    internal IReadOnlyList<Account> Accounts => _accounts;
    internal IReadOnlyList<AccountLinkState> States => _states;
    internal IReadOnlyList<Account?> Active => _active;
    public ExceptionDispatchInfo? Failure { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            await _host.InitializeAsync();
            await RunScenario();
        }
        catch (Exception exception)
        {
            Failure = ExceptionDispatchInfo.Capture(exception);
        }
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    async Task RunScenario()
    {
        await using var scope = _host.Services.CreateAsyncScope();
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

        await ReplayWaits.ReplayAccountProjection(harness.Store, await ReplayWaits.AccountTail(harness.Store), _host.KernelOutput);
        await ReadAndRejectPlaintext(reader, subject);
    }

    async Task VerifyPersonalDataBeforeErasure(AccountReader reader, AccountSubject subject)
    {
        BeforeAccount = await reader.Find(subject);
        BeforeState = await reader.FindLinkState(subject);
        BeforeAccount.ShouldNotBeNull();
        BeforeState.ShouldNotBeNull();
        BeforeAccount.Name.Value.ShouldEqual(ErasureHarness.RenamedPersonalName);
        BeforeAccount.Email.Value.ShouldEqual(EmailAddress);
        BeforeAccount.LinkedUpstreamKeys.Single().IdpSub.Value.ShouldNotEqual(string.Empty);
        BeforeAccount.LinkedUpstreamKeys.Single().Login.Value.ShouldEqual("erasure-spec-login");
        BeforeAccount.LinkedUpstreamKeys.ShouldContainSingleItem();
        BeforeState.Name.Value.ShouldEqual(ErasureHarness.RenamedPersonalName);
        BeforeState.Email.Value.ShouldEqual(EmailAddress);
        BeforeState.Proposals.Values.Single().Subject.Value.ShouldEqual(ProposedSubject);
        BeforeState.Links.ShouldContainSingleItem();
        BeforeState.Logins.Values.Single().Value.ShouldEqual("erasure-spec-login");
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
        account.LinkedUpstreamKeys.Single().Login.Value.ShouldNotEqual("erasure-spec-login");
        state.Name.Value.ShouldNotEqual(ErasureHarness.RenamedPersonalName);
        state.Email.Value.ShouldNotEqual(EmailAddress);
        state.Proposals.Values.Single().Subject.Value.ShouldNotEqual(ProposedSubject);
        state.Logins.Values.Single().Value.ShouldNotEqual("erasure-spec-login");
        _accounts.Add(account);
        _states.Add(state);
        _active.Add(await reader.FindActive(subject));
    }
}
