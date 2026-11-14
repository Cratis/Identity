// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>One majority-durable document is the refresh/revoke ordering point, not an event projection.</summary>
/// <param name="database">The operational database.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="log">The independent sequence authority.</param>
internal sealed class MongoFenceStore(IMongoDatabase database, TimeProvider timeProvider, IIndependentFenceLog log) : IFenceStore, IReservationAuthority
{
    const int Attempts = 8;
    readonly IMongoCollection<BsonDocument> _grants = database.GetCollection<BsonDocument>("grant-fences")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<GrantState?> Read(string grantId, CancellationToken cancellationToken)
    {
        var document = await _grants.Find(new BsonDocument("_id", grantId)).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        return document.Contains("payload") ? ToState(grantId, document) : WithoutPayload(document);
    }

    /// <inheritdoc/>
    public async Task<GrantFence> Commit(string grantId, string subject, RevocationReason reason, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var current = await Read(grantId, cancellationToken);
            if (current?.Fence is { } existing)
            {
                return existing;
            }

            var id = Guid.NewGuid();
            var sequence = await log.Reserve(id, grantId, cancellationToken);
            var fence = new GrantFence(id, sequence, grantId, subject, reason, timeProvider.GetUtcNow());
            if (await CommitReserved(current, fence, cancellationToken))
            {
                return fence;
            }
        }

        throw new RevocationPending();
    }

    /// <inheritdoc/>
    public async Task<GrantState> RecordFamilyStart(string grantId, string tokenId, string subject, DateTimeOffset startedAt, DateTimeOffset absoluteExpiresAt, Guid? link, CancellationToken cancellationToken)
    {
        var current = await Read(grantId, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        if (await log.Tombstone(grantId, cancellationToken) is not null)
        {
            throw new RevocationPending();
        }

        var epoch = await log.RecoveryEpoch(cancellationToken);
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, grantId, subject, startedAt, absoluteExpiresAt);
        var state = new GrantState(grantId, 1, tokenId, null, [entry], epoch, link);
        return await Replace(null, state, cancellationToken) ? state : await Read(grantId, cancellationToken) ?? throw new RevocationPending();
    }

    /// <inheritdoc/>
    public async Task<RedeemOutcome> RedeemAndInstall(string grantId, string consumedId, string successorId, long expectedVersion, CancellationToken cancellationToken)
    {
        var current = await Read(grantId, cancellationToken);
        if (current is null)
        {
            return RedeemOutcome.Unknown;
        }

        if (await IsFenced(current, grantId, cancellationToken))
        {
            return RedeemOutcome.Fenced;
        }

        if (FenceStoreRules.Refusal(current, consumedId, expectedVersion) is { } refusal)
        {
            return refusal;
        }

        var next = current with { Version = current.Version + 1, CurrentRefreshTokenId = successorId };
        return await Replace(current, next, cancellationToken)
            ? RedeemOutcome.Installed
            : FenceStoreRules.AfterLostRace(await Read(grantId, cancellationToken), consumedId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GrantState>> Undelivered(int limit, CancellationToken cancellationToken)
    {
        var documents = await _grants.Find(new BsonDocument("undelivered", true)).Limit(limit).ToListAsync(cancellationToken);
        var states = new List<GrantState>();
        foreach (var document in documents)
        {
            states.Add(await Read(document["_id"].AsString, cancellationToken) ?? throw new RevocationPending());
        }

        return states;
    }

    /// <inheritdoc/>
    public Task ConfirmLog(GrantFence fence, CancellationToken cancellationToken) => Progress(
        fence.GrantId,
        state =>
    {
        if (state.Fence != fence)
        {
            throw new FenceLogConflict();
        }

        return state with { Outbox = state.Outbox.Select(entry => entry.Fence == fence && entry.State == OutboxState.Pending ? entry with { State = OutboxState.LogConfirmed } : entry).ToArray() };
    },
        cancellationToken);

    /// <inheritdoc/>
    public Task Delivered(string grantId, string outboxId, CancellationToken cancellationToken) => Progress(
        grantId,
        state =>
    {
        var entry = state.Outbox.SingleOrDefault(candidate => candidate.Id == outboxId) ?? throw new RevocationPending();
        if (entry.State == OutboxState.Pending)
        {
            throw new RevocationPending();
        }

        return state with { Outbox = state.Outbox.Select(candidate => candidate.Id == outboxId ? candidate with { State = OutboxState.Delivered } : candidate).ToArray() };
    },
        cancellationToken);

    /// <inheritdoc/>
    public async Task<FenceRecord?> Cancel(FenceReservation reservation, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _grants.UpdateOneAsync(
                FenceStoreRules.CancellationFilter(reservation),
                FenceStoreRules.CancellationUpdate(reservation),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
            return FenceStoreRules.IsCommitted(result) ? null : throw new RevocationPending();
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            return await RecordCommittedFence(reservation, cancellationToken);
        }
    }

    /// <summary>Attempts one writer using its original independent reservation.</summary>
    /// <param name="current">The expected authority revision.</param>
    /// <param name="fence">The originally reserved permanent denial.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether the conditional write committed.</returns>
    /// <exception cref="RevocationPending">Cancellation won or the commit remains uncertain.</exception>
    internal async Task<bool> CommitReserved(GrantState? current, GrantFence fence, CancellationToken cancellationToken)
    {
        var entries = (current?.Outbox ?? []).Concat(FenceRecordFactory.Entries(fence)).ToArray();
        var next = new GrantState(fence.GrantId, (current?.Version ?? 0) + 1, null, fence, entries, current?.RecoveryEpoch ?? 0);
        if (await Replace(current, next, cancellationToken))
        {
            return true;
        }

        var cancelled = await _grants.Find(new BsonDocument { { "_id", fence.GrantId }, { "cancelledReservations", fence.Id.ToString("D") } }).AnyAsync(cancellationToken);
        if (cancelled)
        {
            // Do not retry a cancelled operation under a newly allocated identity.
            throw new RevocationPending();
        }

        return false;
    }

    /// <summary>Atomically replaces authority while preserving cancellation markers.</summary>
    /// <param name="current">The expected authority revision.</param>
    /// <param name="next">The new authority revision.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="replayEpoch">An optional atomic delivery-reset marker.</param>
    /// <returns>Whether the conditional write committed.</returns>
    /// <exception cref="RevocationPending">The write was not acknowledged.</exception>
    internal async Task<bool> Replace(GrantState? current, GrantState next, CancellationToken cancellationToken, long? replayEpoch = null)
    {
        try
        {
            // $set preserves all cancellation markers. Replacing the document could resurrect a delayed writer.
            var result = await _grants.UpdateOneAsync(
                FenceStoreRules.ReplacementFilter(current, next),
                new BsonDocument("$set", FenceStoreRules.ReplacementDocument(next, replayEpoch)),
                new UpdateOptions { IsUpsert = current is null },
                cancellationToken);
            return FenceStoreRules.IsCommitted(result);
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            return false;
        }
    }

    static bool IsDuplicateKey(MongoWriteException exception) => exception.WriteError.Category == ServerErrorCategory.DuplicateKey;

    /// <summary>A cancellation can win before the first operational write. It must survive that write.</summary>
    /// <param name="document">The stored document.</param>
    /// <returns>No state when only cancellation markers exist.</returns>
    /// <exception cref="RevocationPending">The document carries neither payload nor cancellation.</exception>
    static GrantState? WithoutPayload(BsonDocument document) => FenceStoreRules.IsCancellationOnly(document) ? null : throw new RevocationPending();

    static GrantState ToState(string grantId, BsonDocument document)
    {
        var state = JsonSerializer.Deserialize<GrantState>(document["payload"].AsString) ?? throw new RevocationPending();
        return FenceStoreRules.IsConsistent(state, grantId, document) ? state : throw new RevocationPending();
    }

    async Task<bool> IsFenced(GrantState current, string grantId, CancellationToken cancellationToken) =>
        FenceStoreRules.IsFenced(current) ||
        FenceStoreRules.IsStaleEpoch(current, await log.RecoveryEpoch(cancellationToken)) ||
        await log.Tombstone(grantId, cancellationToken) is not null;

    async Task<FenceRecord?> RecordCommittedFence(FenceReservation reservation, CancellationToken cancellationToken)
    {
        var state = await Read(reservation.GrantId, cancellationToken) ?? throw new RevocationPending();
        return FenceStoreRules.IsCommittedBy(state, reservation) ? FenceRecordFactory.Record(state) : throw new RevocationPending();
    }

    async Task Progress(string grantId, Func<GrantState, GrantState> update, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var state = await Read(grantId, cancellationToken) ?? throw new RevocationPending();
            var next = update(state) with { Version = state.Version + 1 };
            if (await Replace(state, next, cancellationToken))
            {
                return;
            }
        }

        throw new RevocationPending();
    }
}
