// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>Denial-only recovery; scans retained tombstones even below the operational prefix.</summary>
/// <param name="database">The restorable operational store.</param>
/// <param name="store">The operational grant authority.</param>
/// <param name="gate">The independent epoch guard.</param>
internal sealed class MongoFenceRecovery(IMongoDatabase database, MongoFenceStore store, RecoveryGate gate) : IRecoveryStore
{
    readonly IMongoCollection<BsonDocument> _checkpoints = database.GetCollection<BsonDocument>("fence-recovery")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);
    readonly IMongoCollection<BsonDocument> _grants = database.GetCollection<BsonDocument>("grant-fences")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<RecoveryCheckpoint?> Checkpoint(CancellationToken cancellationToken)
    {
        var document = await _checkpoints.Find(new BsonDocument("_id", "checkpoint")).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : JsonSerializer.Deserialize<RecoveryCheckpoint>(document["payload"].AsString) ?? throw new RevocationPending();
    }

    /// <inheritdoc/>
    public Task<bool> HasState(CancellationToken cancellationToken) => _grants.Find(new BsonDocument()).AnyAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> Contains(FenceRecord record, CancellationToken cancellationToken)
    {
        var state = await store.Read(record.Fence.GrantId, cancellationToken);
        if (state?.Fence is null)
        {
            return false;
        }

        Verify(state, record);
        return true;
    }

    /// <inheritdoc/>
    public async Task Apply(FenceRecord record, bool resetDelivery, CancellationToken cancellationToken)
    {
        FenceRecordFactory.Validate(record);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var marker = await _grants.Find(new BsonDocument("_id", record.Fence.GrantId)).FirstOrDefaultAsync(cancellationToken);
            var reset = resetDelivery && marker?.GetValue("replayEpoch", -1L).AsInt64 != gate.Epoch;
            var current = await store.Read(record.Fence.GrantId, cancellationToken);
            if (current?.Fence is not null)
            {
                Verify(current, record);
                if (!reset)
                {
                    return;
                }
            }

            var retained = (current?.Outbox ?? []).Where(entry => entry.Fence is null);
            var intents = record.Intents!.Select(entry => entry with { State = OutboxState.LogConfirmed });
            var next = new GrantState(record.Fence.GrantId, (current?.Version ?? 0) + 1, null, record.Fence, [.. retained, .. intents], current?.RecoveryEpoch ?? 0);
            if (await store.Replace(current, next, cancellationToken, resetDelivery ? gate.Epoch : null))
            {
                return;
            }
        }

        throw new RevocationPending();
    }

    /// <inheritdoc/>
    public async Task Save(RecoveryCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        var result = await _checkpoints.UpdateOneAsync(
            new BsonDocument("_id", "checkpoint"),
            new BsonDocument("$set", new BsonDocument("payload", JsonSerializer.Serialize(checkpoint))),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
        if (!result.IsAcknowledged)
        {
            throw new RevocationPending();
        }
    }

    static void Verify(GrantState state, FenceRecord record)
    {
        var actual = FenceRecordFactory.Record(state);
        if (actual.Fence != record.Fence || !actual.Intents.SequenceEqual(record.Intents))
        {
            throw new FenceLogConflict();
        }
    }
}
