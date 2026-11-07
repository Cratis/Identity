// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_a_restore_identity_is_reused(IdentityHost host) : IAsyncLifetime
{
    Exception? _changedKind;
    Exception? _anotherHost;
    Exception? _completed;
    long _first;
    long _retry;

    public async ValueTask InitializeAsync()
    {
        var log = await FenceLogDatabase.Create(host.Database);
        await log.Acquire("host-one", CancellationToken.None);
        _first = await log.RegisterRestore("restore-test", RestoreKind.Operational, "host-one", CancellationToken.None);
        _retry = await log.RegisterRestore("restore-test", RestoreKind.Operational, "host-one", CancellationToken.None);
        _changedKind = await Catch.Exception(() => log.RegisterRestore("restore-test", RestoreKind.Chronicle, "host-one", CancellationToken.None));
        _anotherHost = await Catch.Exception(() => log.RegisterRestore("restore-test", RestoreKind.Operational, "host-two", CancellationToken.None));
        await log.CompleteRestore("host-one", "restore-test", false, CancellationToken.None);
        _completed = await Catch.Exception(() => log.RegisterRestore("restore-test", RestoreKind.Operational, "host-one", CancellationToken.None));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_allocate_the_epoch_only_once_for_the_live_retry() => _retry.ShouldEqual(_first);
    [Fact] void should_refuse_a_changed_restore_kind() => _changedKind.ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_refuse_identity_reuse_by_another_host() => _anotherHost.ShouldBeOfExactType<FenceLogConflict>();
    [Fact] void should_refuse_a_completed_declaration() => _completed.ShouldBeOfExactType<FenceLogConflict>();
}
