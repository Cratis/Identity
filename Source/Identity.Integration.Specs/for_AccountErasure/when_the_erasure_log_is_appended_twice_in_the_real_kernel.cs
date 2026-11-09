// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_AccountErasure.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AccountErasure;

[Collection(IdentityCollection.Name)]
public class when_the_erasure_log_is_appended_twice_in_the_real_kernel(IdentityHost host) : IAsyncLifetime
{
    object[] _events = [];
    Exception _conflict = null!;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var harness = new ErasureHarness(scope.ServiceProvider);
        var account = await harness.TombstonedAccount();
        var erasure = ErasureId.New();
        var requested = new AccountErasureRequested(erasure, DateTimeOffset.UnixEpoch);
        var failed = new AccountErasureFailed(erasure, ErasureStages.DestroySubjectKeys, "erasure_key_destruction_failed");

        // Fresh log instances have no memory: only the kernel's keyed append can deduplicate.
        await harness.Log.Append(account, requested);
        await harness.Log.Append(account, requested);
        _conflict = await Catch.Exception(() => harness.Log.Append(account, requested with { RequestedAt = DateTimeOffset.UnixEpoch.AddDays(1) }));
        await harness.Log.Append(account, failed);
        _events = (await harness.Store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, new EventSourceId(account.Value))).Select(@event => @event.Content).ToArray();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_store_the_request_once() => _events.OfType<AccountErasureRequested>().ShouldContainSingleItem();
    [Fact] void should_refuse_changed_content_for_the_same_request() => _conflict.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_store_the_first_failure() => _events.OfType<AccountErasureFailed>().ShouldContainSingleItem();
}
