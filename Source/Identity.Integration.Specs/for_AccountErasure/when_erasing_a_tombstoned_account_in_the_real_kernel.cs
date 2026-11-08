// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_AccountErasure.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure;

[Collection(IdentityCollection.Name)]
public class when_erasing_a_tombstoned_account_in_the_real_kernel(IdentityHost host) : IAsyncLifetime
{
    ErasureRunResult _first = null!;
    ErasureRunResult _second = null!;
    bool _firstRequestAppended;
    bool _repeatRequestAppended;
    object[] _events = [];
    ErasureState _state = null!;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var harness = new ErasureHarness(scope.ServiceProvider);
        var account = await harness.TombstonedAccount();
        var erasure = ErasureId.New();
        _firstRequestAppended = await harness.Requests.Request(new ClaimsPrincipal(), account, erasure);
        _repeatRequestAppended = await harness.Requests.Request(new ClaimsPrincipal(), account, erasure);
        _first = await harness.Coordinator.Run(account, erasure);

        // A second coordinator with no delivery memory resumes from the stream and must not record again.
        _second = await harness.Coordinator.Run(account, erasure);
        _events = (await harness.Store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, new EventSourceId(account.Value))).Select(@event => @event.Content).ToArray();
        _state = await harness.Log.Read(account);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_accept_the_request_once() => _firstRequestAppended.ShouldBeTrue();
    [Fact] void should_treat_the_repeat_as_idempotent() => _repeatRequestAppended.ShouldBeFalse();
    [Fact] void should_append_one_request_fact() => _events.OfType<AccountErasureRequested>().ShouldContainSingleItem();
    [Fact] void should_complete_the_first_run() => _first.Completed.ShouldBeTrue();
    [Fact] void should_complete_the_second_run_without_rework() => _second.Completed.ShouldBeTrue();
    [Fact] void should_record_completion_exactly_once() => _events.OfType<AccountErasureRecorded>().ShouldContainSingleItem();
    [Fact] void should_carry_the_boundary_version_in_the_completion() => _events.OfType<AccountErasureRecorded>().Single().BoundaryVersion.ShouldEqual(ErasureHarness.SpecBoundary);
    [Fact] void should_not_record_a_failure() => _events.OfType<AccountErasureFailed>().ShouldBeEmpty();
    [Fact] void should_fold_to_completed() => _state.Completed.ShouldBeTrue();
    [Fact] void should_keep_the_tombstone() => _events.OfType<AccountTombstoneRecorded>().ShouldContainSingleItem();
}
