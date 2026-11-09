// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_progress_is_saved_after_an_operational_rollback(IdentityHost host) : IAsyncLifetime
{
    readonly RecoveryCheckpoint _backup = new("test-custody", 1, "previous-boot");
    RecoveryCheckpoint? _retained;
    Exception? _staleSave;
    Exception? _staleInsert;

    public async ValueTask InitializeAsync()
    {
        var database = host.Database.Client.GetDatabase($"checkpoint-race-{Guid.NewGuid():N}");
        var recovery = new MongoFenceRecovery(database, host.Services.GetRequiredService<MongoFenceStore>(), new RecoveryGate());
        var advanced = _backup with { AppliedThrough = 2, Nonce = "current-boot" };
        await recovery.Save(null, _backup, CancellationToken.None);
        await recovery.Save(_backup, advanced, CancellationToken.None);
        await database.GetCollection<BsonDocument>("fence-recovery").UpdateOneAsync(
            new BsonDocument("_id", "checkpoint"), new BsonDocument("$set", new BsonDocument("payload", JsonSerializer.Serialize(_backup))));
        _staleSave = await Catch.Exception(() => recovery.Save(advanced, advanced with { AppliedThrough = 3 }, CancellationToken.None));
        _staleInsert = await Catch.Exception(() => recovery.Save(null, advanced, CancellationToken.None));
        _retained = await recovery.Checkpoint(CancellationToken.None);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_reject_progress_based_on_the_lost_checkpoint() => _staleSave.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_upsert_over_a_restored_checkpoint() => _staleInsert.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_retain_the_restore_evidence() => _retained.ShouldEqual(_backup);
}
