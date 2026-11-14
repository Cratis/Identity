// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_AccountErasure.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure;

/// <summary>
/// The control for the erasure replay spec: with no erasure, a replay must leave the account as it was. If this fails,
/// replay is unusable in this environment (client and kernel skew), not because of key destruction.
/// </summary>
/// <param name="host">The host with its own kernel.</param>
public class when_replaying_the_account_projection_without_erasure(ErasureReplayHost host) : IClassFixture<ErasureReplayHost>, IAsyncLifetime
{
    const string EmailAddress = "baseline@example.test";
    Account? _before;
    Account? _after;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var harness = new ErasureHarness(scope.ServiceProvider);
        var subject = await harness.RegisteredAccount();
        (await harness.Store.EventLog.Append(subject.Value, new AccountEmailChanged(EmailAddress))).IsSuccess.ShouldBeTrue();
        var tail = await ReplayWaits.AccountTail(harness.Store);
        await harness.Store.Projections.WaitTillReachesEventSequenceNumber<AccountProjection>(tail, ReplayWaits.Bound);
        var reader = new AccountReader(harness.Store.ReadModels);
        _before = await reader.Find(subject);

        await ReplayWaits.ReplayAccountProjection(harness.Store, tail, host.KernelOutput);
        _after = await reader.Find(subject);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_have_read_the_account_before_the_replay() => _before.ShouldNotBeNull();
    [Fact] void should_read_the_account_after_the_replay() => _after.ShouldNotBeNull();
    [Fact] void should_keep_the_name() => _after.Name.Value.ShouldEqual(ErasureHarness.RenamedPersonalName);
    [Fact] void should_keep_the_email() => _after.Email.Value.ShouldEqual(EmailAddress);
    [Fact] void should_keep_the_account_active() => _after.Status.ShouldEqual(AccountStatus.Active);
}
