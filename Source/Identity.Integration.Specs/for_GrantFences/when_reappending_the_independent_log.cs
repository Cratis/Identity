// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_reappending_the_independent_log(IdentityHost host) : IAsyncLifetime
{
    GrantFence _fence;
    long _first;
    long _retry;
    Exception? _conflict;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host);
        var store = new MongoFenceStore(host.Database, TimeProvider.System, log);
        _fence = await store.Commit(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), RevocationReason.Revoked, CancellationToken.None);
        var state = await store.Read(_fence.GrantId, CancellationToken.None);
        var record = FenceRecordFactory.Record(state);
        _first = await log.Append(record, CancellationToken.None);
        _retry = await log.Append(record, CancellationToken.None);
        _conflict = await Catch.Exception(() => log.Append(record with { Fence = _fence with { Reason = RevocationReason.RefreshTokenReused } }, CancellationToken.None));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_return_the_original_sequence() => _first.ShouldEqual(_fence.Sequence);
    [Fact] void should_be_idempotent_across_adapter_instances() => _retry.ShouldEqual(_first);
    [Fact] void should_refuse_different_content_for_the_same_identity() => _conflict.ShouldBeOfExactType<FenceLogConflict>();
}
