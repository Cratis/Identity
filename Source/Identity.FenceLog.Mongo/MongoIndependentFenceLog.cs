// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>
/// Majority-durable custody on an independently provisioned replica set. Never creates its metadata marker.
/// </summary>
/// <param name="database">The independent database, never the operational database.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="verified">The host startup boundary proof; omitted only by native adapter specifications.</param>
public sealed class MongoIndependentFenceLog(Func<IMongoDatabase> database, TimeProvider timeProvider, Func<bool>? verified = null) : IRecoveryLog
{
    readonly Lazy<IMongoCollection<BsonDocument>> _entryCollection = new(() => database().GetCollection<BsonDocument>("fence-reservations")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority));
    readonly Lazy<IMongoCollection<BsonDocument>> _metadataCollection = new(() => database().GetCollection<BsonDocument>("fence-custody")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority));

    /// <summary>Constructs an explicitly provisioned native test adapter.</summary>
    /// <param name="database">The independent test database.</param>
    /// <param name="timeProvider">The test clock.</param>
    internal MongoIndependentFenceLog(IMongoDatabase database, TimeProvider timeProvider) : this(() => database, timeProvider)
    {
    }

    IMongoCollection<BsonDocument> _entries => verified?.Invoke() != false ? _entryCollection.Value : throw new RevocationPending();
    IMongoCollection<BsonDocument> _metadata => verified?.Invoke() != false ? _metadataCollection.Value : throw new RevocationPending();

    /// <inheritdoc/>
    public async Task<long> Reserve(Guid fenceId, string grantId, CancellationToken cancellationToken)
    {
        if (fenceId == Guid.Empty || string.IsNullOrWhiteSpace(grantId))
        {
            throw new FenceLogConflict();
        }

        using var session = await database().Client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(
            async (transaction, token) =>
        {
            var existing = await _entries.Find(transaction, Identity(fenceId)).FirstOrDefaultAsync(token);
            if (existing is not null)
            {
                if (existing["grantId"].AsString != grantId)
                {
                    throw new FenceLogConflict();
                }

                return existing["sequence"].AsInt64;
            }

            var metadata = await _metadata.FindOneAndUpdateAsync(
                transaction,
                new BsonDocument("_id", "metadata"),
                new BsonDocument("$inc", new BsonDocument("allocated", 1L)),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                token) ?? throw new RevocationPending();
            var sequence = metadata["allocated"].AsInt64;
            await _entries.InsertOneAsync(
                transaction,
                new BsonDocument
                {
                    { "_id", fenceId.ToString("D") }, { "sequence", sequence }, { "grantId", grantId },
                    { "state", (int)ReservationState.Reserved }, { "reservedAt", timeProvider.GetUtcNow().UtcDateTime }
                },
                cancellationToken: token);
            return sequence;
        },
            Options(),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long> Append(FenceRecord record, CancellationToken cancellationToken)
    {
        FenceRecordFactory.Validate(record);
        var filter = Identity(record.Fence.Id).Add("sequence", record.Fence.Sequence).Add("grantId", record.Fence.GrantId)
            .Add("state", (int)ReservationState.Reserved);
        await _entries.UpdateOneAsync(filter, Filled(record), cancellationToken: cancellationToken);
        var existing = await _entries.Find(Identity(record.Fence.Id)).FirstOrDefaultAsync(cancellationToken) ?? throw new FenceLogConflict();
        VerifyFilled(existing, record);
        return record.Fence.Sequence;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FenceReservation>> Unresolved(CancellationToken cancellationToken)
    {
        var filter = new BsonDocument("state", new BsonDocument("$in", new BsonArray { (int)ReservationState.Reserved, (int)ReservationState.Fencing }));
        var entries = await _entries.Find(filter).Sort(new BsonDocument("sequence", 1)).Limit(100).ToListAsync(cancellationToken);
        return entries.Select(document => new FenceReservation(
            Guid.Parse(document["_id"].AsString),
            document["sequence"].AsInt64,
            document["grantId"].AsString,
            State(document),
            new DateTimeOffset(document["reservedAt"].ToUniversalTime()))).ToArray();
    }

    /// <inheritdoc/>
    public async Task<ReservationState> Fence(FenceReservation reservation, CancellationToken cancellationToken)
    {
        var filter = Identity(reservation.Id).Add("sequence", reservation.Sequence).Add("grantId", reservation.GrantId)
            .Add("state", (int)ReservationState.Reserved);
        await _entries.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument("state", (int)ReservationState.Fencing)), cancellationToken: cancellationToken);
        var document = await _entries.Find(Identity(reservation.Id)).FirstOrDefaultAsync(cancellationToken) ?? throw new RevocationPending();
        if (document["sequence"].AsInt64 != reservation.Sequence || document["grantId"].AsString != reservation.GrantId)
        {
            throw new FenceLogConflict();
        }

        return State(document);
    }

    /// <inheritdoc/>
    public async Task Resolve(FenceReservation reservation, FenceRecord? committed, CancellationToken cancellationToken)
    {
        if (committed is not null)
        {
            FenceRecordFactory.Validate(committed);
            if (!MatchesReservation(committed.Fence, reservation))
            {
                throw new FenceLogConflict();
            }
        }

        var filter = Identity(reservation.Id).Add("sequence", reservation.Sequence).Add("state", (int)ReservationState.Fencing);
        var update = committed is null
            ? new BsonDocument("$set", new BsonDocument("state", (int)ReservationState.Voided))
            : Filled(committed);
        await _entries.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        var document = await _entries.Find(Identity(reservation.Id)).FirstOrDefaultAsync(cancellationToken) ?? throw new RevocationPending();
        if (committed is not null)
        {
            VerifyFilled(document, committed);
        }
        else if (State(document) != ReservationState.Voided)
        {
            throw new RevocationPending();
        }
    }

    /// <inheritdoc/>
    public async Task<long> Watermark(CancellationToken cancellationToken)
    {
        // Reserve commits the entry and counter together; read both from the same snapshot.
        using var session = await database().Client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(
            async (transaction, token) =>
        {
            var entries = await _entries.Find(transaction, new BsonDocument()).Sort(new BsonDocument("sequence", 1)).ToListAsync(token);
            var prefix = 0L;
            var expected = 0L;
            foreach (var entry in entries)
            {
                expected++;
                if (entry["sequence"].AsInt64 != expected)
                {
                    throw new RevocationPending();
                }

                if (State(entry) == ReservationState.Filled)
                {
                    _ = Record(entry);
                }

                if (State(entry) == ReservationState.Epoch)
                {
                    _ = EpochIntent(entry);
                }

                if (prefix == expected - 1 && State(entry) is ReservationState.Filled or ReservationState.Voided or ReservationState.Epoch)
                {
                    prefix++;
                }
            }

            if (expected != (await Metadata(token, transaction))["allocated"].AsInt64)
            {
                throw new RevocationPending();
            }

            return prefix;
        },
            Options(),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FenceRecord>> Read(long after, CancellationToken cancellationToken)
    {
        var filter = new BsonDocument { { "sequence", new BsonDocument("$gt", after) }, { "state", (int)ReservationState.Filled } };
        var documents = await _entries.Find(filter).Sort(new BsonDocument("sequence", 1)).ToListAsync(cancellationToken);
        var records = documents.Select(Record).ToArray();
        if (records.Select(record => record.Fence.GrantId).Distinct().Count() != records.Length)
        {
            throw new FenceLogConflict();
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task<FenceRecord?> Tombstone(string grantId, CancellationToken cancellationToken)
    {
        var documents = await _entries.Find(new BsonDocument { { "grantId", grantId }, { "state", (int)ReservationState.Filled } }).Limit(2).ToListAsync(cancellationToken);
        return documents.Count switch
        {
            0 => null,
            1 => Record(documents[0]),
            _ => throw new FenceLogConflict()
        };
    }

    /// <inheritdoc/>
    public async Task<RecoveryCustody> Custody(CancellationToken cancellationToken)
    {
        var metadata = await Metadata(cancellationToken);
        return new(
            metadata["instanceId"].AsString,
            metadata.GetValue("appliedThrough", 0L).AsInt64,
            metadata.GetValue("nonce", string.Empty).AsString,
            metadata["epoch"].AsInt64,
            metadata.GetValue("clearedEpoch", 0L).AsInt64);
    }

    /// <inheritdoc/>
    public async Task<bool> Acquire(string owner, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var filter = new BsonDocument
        {
            { "_id", "metadata" },
            { "$or", new BsonArray { new BsonDocument("owner", owner), new BsonDocument("leaseUntil", new BsonDocument("$lt", now)), new BsonDocument("owner", new BsonDocument("$exists", false)) } }
        };
        var update = new BsonDocument("$set", new BsonDocument { { "owner", owner }, { "leaseUntil", now.AddMinutes(1) } });
        var result = await _metadata.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }

    /// <inheritdoc/>
    public async Task Checkpoint(string owner, RecoveryCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        var filter = Owned(owner).Add("instanceId", checkpoint.InstanceId).Add("appliedThrough", new BsonDocument("$not", new BsonDocument("$gt", checkpoint.AppliedThrough)));
        var result = await _metadata.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument { { "appliedThrough", checkpoint.AppliedThrough }, { "nonce", checkpoint.Nonce } }), cancellationToken: cancellationToken);
        RequireMatched(result);
    }

    /// <inheritdoc/>
    public async Task CompleteRestore(string owner, string restoreId, bool restrictionsReestablished, CancellationToken cancellationToken)
    {
        using var session = await database().Client.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(
            async (transaction, token) =>
        {
            var declaration = await _metadata.Find(transaction, new BsonDocument("_id", $"restore:{restoreId}")).FirstOrDefaultAsync(token) ?? throw new RevocationPending();
            var result = await _metadata.UpdateOneAsync(
                transaction,
                Owned(owner).Add("epoch", declaration["epoch"]),
                new BsonDocument("$set", new BsonDocument("clearedEpoch", restrictionsReestablished ? declaration["epoch"].AsInt64 : -1L)),
                cancellationToken: token);
            RequireMatched(result);
            await _metadata.UpdateOneAsync(
                transaction,
                new BsonDocument("_id", $"restore:{restoreId}"),
                new BsonDocument("$set", new BsonDocument("completed", true)),
                cancellationToken: token);
            return true;
        },
            Options(),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FenceReservation>> Voided(CancellationToken cancellationToken)
    {
        var entries = await _entries.Find(new BsonDocument("state", (int)ReservationState.Voided)).Sort(new BsonDocument("sequence", 1)).ToListAsync(cancellationToken);
        return entries.Select(document => new FenceReservation(
            Guid.Parse(document["_id"].AsString),
            document["sequence"].AsInt64,
            document["grantId"].AsString,
            ReservationState.Voided,
            new DateTimeOffset(document["reservedAt"].ToUniversalTime()))).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecoveryEpochIntent>> EpochIntents(CancellationToken cancellationToken)
    {
        var documents = await _entries.Find(new BsonDocument("state", (int)ReservationState.Epoch)).Sort(new BsonDocument("sequence", 1)).ToListAsync(cancellationToken);
        return documents.Select(EpochIntent).ToArray();
    }

    /// <inheritdoc/>
    public async Task ConfirmPublication(string owner, long publishedThrough, CancellationToken cancellationToken)
    {
        var result = await _metadata.UpdateOneAsync(Owned(owner), new BsonDocument("$max", new BsonDocument("publishedThrough", publishedThrough)), cancellationToken: cancellationToken);
        RequireMatched(result);
    }

    /// <inheritdoc/>
    public async Task<long> AllocatedThrough(CancellationToken cancellationToken) => (await Metadata(cancellationToken))["allocated"].AsInt64;

    /// <inheritdoc/>
    public async Task<long> RecoveryEpoch(CancellationToken cancellationToken) => (await Metadata(cancellationToken))["epoch"].AsInt64;

    /// <inheritdoc/>
    public Task<long> AdvanceEpoch(string restoreId, CancellationToken cancellationToken) => throw new RevocationPending();

    /// <inheritdoc/>
    public async Task<long> RegisterRestore(string restoreId, RestoreKind kind, string owner, CancellationToken cancellationToken)
    {
        RequireDeclaration(restoreId, kind);

        using var session = await database().Client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(
            async (transaction, token) =>
        {
            var id = $"restore:{restoreId}";
            var existing = await _metadata.Find(transaction, new BsonDocument("_id", id)).FirstOrDefaultAsync(token);
            if (existing is not null)
            {
                RequireRetry(existing, kind, owner);

                return existing["epoch"].AsInt64;
            }

            var metadata = await _metadata.FindOneAndUpdateAsync(
                transaction,
                Owned(owner),
                new BsonDocument("$inc", new BsonDocument { { "epoch", 1L }, { "allocated", 1L } }),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                token) ?? throw new RevocationPending();
            var epoch = metadata["epoch"].AsInt64;
            var intent = new RecoveryEpochIntent(restoreId, epoch, timeProvider.GetUtcNow(), Guid.NewGuid(), metadata["allocated"].AsInt64);
            await _metadata.InsertOneAsync(transaction, new BsonDocument { { "_id", id }, { "owner", owner }, { "kind", (int)kind }, { "epoch", epoch } }, cancellationToken: token);
            await _entries.InsertOneAsync(
                transaction,
                new BsonDocument
                {
                    { "_id", intent.FenceId.ToString("D") }, { "sequence", intent.FenceSequence }, { "state", (int)ReservationState.Epoch },
                    { "payload", JsonSerializer.Serialize(intent) }
                },
                cancellationToken: token);
            return epoch;
        },
            Options(),
            cancellationToken);
    }

    /// <summary>Reads and validates existing independent custody metadata.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="session">An optional consistent read transaction.</param>
    /// <returns>The existing metadata; never an initialized replacement.</returns>
    /// <exception cref="RevocationPending">Custody is missing or inconsistent.</exception>
    internal async Task<BsonDocument> Metadata(CancellationToken cancellationToken, IClientSessionHandle? session = null)
    {
        var filter = new BsonDocument("_id", "metadata");
        var query = session is null ? _metadata.Find(filter) : _metadata.Find(session, filter);
        var metadata = await query.FirstOrDefaultAsync(cancellationToken) ?? throw new RevocationPending();
        var allocated = metadata["allocated"].AsInt64;
        RequireBound(metadata.GetValue("appliedThrough", 0L).AsInt64, allocated);
        RequireBound(metadata.GetValue("publishedThrough", 0L).AsInt64, allocated);
        if (allocated < 0 || metadata["epoch"].AsInt64 < 0)
        {
            throw new RevocationPending();
        }

        return metadata;
    }

    static bool MatchesReservation(GrantFence fence, FenceReservation reservation) =>
        fence.Id == reservation.Id && fence.Sequence == reservation.Sequence && fence.GrantId == reservation.GrantId;

    static void RequireDeclaration(string restoreId, RestoreKind kind)
    {
        if (string.IsNullOrWhiteSpace(restoreId))
        {
            throw new RevocationPending();
        }

        if (kind == RestoreKind.None || !Enum.IsDefined(kind))
        {
            throw new RevocationPending();
        }
    }

    static void RequireRetry(BsonDocument declaration, RestoreKind kind, string owner)
    {
        if (declaration["kind"].AsInt32 != (int)kind || declaration["owner"].AsString != owner)
        {
            throw new FenceLogConflict();
        }

        if (declaration.GetValue("completed", false).AsBoolean)
        {
            throw new FenceLogConflict();
        }
    }

    static void RequireMatched(UpdateResult result)
    {
        if (!result.IsAcknowledged || result.MatchedCount != 1)
        {
            throw new RevocationPending();
        }
    }

    static void RequireBound(long prefix, long allocated)
    {
        if (prefix < 0 || prefix > allocated)
        {
            throw new RevocationPending();
        }
    }

    static TransactionOptions Options() => new(readConcern: ReadConcern.Snapshot, readPreference: ReadPreference.Primary, writeConcern: WriteConcern.WMajority);
    static BsonDocument Identity(Guid id) => new("_id", id.ToString("D"));
    static ReservationState State(BsonDocument document)
    {
        var state = (ReservationState)document["state"].AsInt32;
        return Enum.IsDefined(state) ? state : throw new RevocationPending();
    }

    static BsonDocument Filled(FenceRecord record) => new("$set", new BsonDocument
    {
        { "state", (int)ReservationState.Filled }, { "payload", JsonSerializer.Serialize(record) }
    });

    static RecoveryEpochIntent EpochIntent(BsonDocument document)
    {
        var intent = JsonSerializer.Deserialize<RecoveryEpochIntent>(document["payload"].AsString) ?? throw new FenceLogConflict();
        RequireEntryIdentity(document, intent.FenceId, intent.FenceSequence);
        if (intent.Epoch <= 0)
        {
            throw new FenceLogConflict();
        }

        return intent;
    }

    static FenceRecord Record(BsonDocument document)
    {
        var record = JsonSerializer.Deserialize<FenceRecord>(document["payload"].AsString) ?? throw new FenceLogConflict();
        FenceRecordFactory.Validate(record);
        RequireEntryIdentity(document, record.Fence.Id, record.Fence.Sequence);
        if (record.Fence.GrantId != document["grantId"].AsString)
        {
            throw new FenceLogConflict();
        }

        return record;
    }

    static void RequireEntryIdentity(BsonDocument document, Guid id, long sequence)
    {
        if (document["_id"].AsString != id.ToString("D") || document["sequence"].AsInt64 != sequence)
        {
            throw new FenceLogConflict();
        }
    }

    static bool SameIntents(FenceRecord actual, FenceRecord expected) =>
        actual.IntentIds.SequenceEqual(expected.IntentIds) && actual.Intents.SequenceEqual(expected.Intents);

    static void VerifyFilled(BsonDocument document, FenceRecord expected)
    {
        if (State(document) != ReservationState.Filled)
        {
            throw new RevocationPending();
        }

        var actual = Record(document);
        if (actual.Fence != expected.Fence || !SameIntents(actual, expected))
        {
            throw new FenceLogConflict();
        }
    }

    BsonDocument Owned(string owner) => new() { { "_id", "metadata" }, { "owner", owner }, { "leaseUntil", new BsonDocument("$gt", timeProvider.GetUtcNow().UtcDateTime) } };
}
