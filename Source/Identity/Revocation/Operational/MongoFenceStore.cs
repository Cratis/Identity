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

        if (!document.Contains("payload"))
        {
            // A cancellation can win before the first operational write. It must survive that write.
            if (!document.TryGetValue("cancelledReservations", out var cancelled) || cancelled.AsBsonArray.Count == 0)
            {
                throw new RevocationPending();
            }

            return null;
        }

        var state = JsonSerializer.Deserialize<GrantState>(document["payload"].AsString) ?? throw new RevocationPending();
        if (!GrantStateValidation.IsKnown(state) || state.GrantId != grantId || state.Version != document["version"].AsInt64 ||
            (state.Fence is not null) != document["fenced"].AsBoolean ||
            state.CurrentRefreshTokenId != (document["currentRefreshTokenId"].IsBsonNull ? null : document["currentRefreshTokenId"].AsString) ||
            state.Outbox.Any(entry => entry.State != OutboxState.Delivered) != document["undelivered"].AsBoolean)
        {
            throw new RevocationPending();
        }

        return state;
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
    public async Task<GrantState> RecordFamilyStart(string grantId, string tokenId, string subject, DateTimeOffset startedAt, DateTimeOffset absoluteExpiresAt, CancellationToken cancellationToken)
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
        var state = new GrantState(grantId, 1, tokenId, null, [entry], epoch);
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

        if (current.Fence is not null || current.RecoveryEpoch != await log.RecoveryEpoch(cancellationToken) || await log.Tombstone(grantId, cancellationToken) is not null)
        {
            return RedeemOutcome.Fenced;
        }

        if (current.CurrentRefreshTokenId != consumedId)
        {
            return RedeemOutcome.NotCurrent;
        }

        if (current.Version != expectedVersion)
        {
            return RedeemOutcome.VersionConflict;
        }

        var next = current with { Version = current.Version + 1, CurrentRefreshTokenId = successorId };
        if (await Replace(current, next, cancellationToken))
        {
            return RedeemOutcome.Installed;
        }

        var winner = await Read(grantId, cancellationToken);
        return winner switch
        {
            null => RedeemOutcome.Unknown,
            { Fence: not null } => RedeemOutcome.Fenced,
            _ when winner.CurrentRefreshTokenId != consumedId => RedeemOutcome.NotCurrent,
            _ => RedeemOutcome.VersionConflict
        };
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
            var filter = new BsonDocument { { "_id", reservation.GrantId }, { "fenceId", new BsonDocument("$ne", reservation.Id.ToString("D")) } };
            var result = await _grants.UpdateOneAsync(
                filter,
                new BsonDocument("$addToSet", new BsonDocument("cancelledReservations", reservation.Id.ToString("D"))),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
            if (!result.IsAcknowledged || (result.MatchedCount != 1 && result.UpsertedId is null))
            {
                throw new RevocationPending();
            }

            return null;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var state = await Read(reservation.GrantId, cancellationToken) ?? throw new RevocationPending();
            if (state.Fence is not { } fence || fence.Id != reservation.Id || fence.Sequence != reservation.Sequence)
            {
                throw new RevocationPending();
            }

            return FenceRecordFactory.Record(state);
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
        var document = new BsonDocument
        {
            { "version", next.Version }, { "fenced", next.Fence is not null },
            { "fenceId", next.Fence is null ? BsonNull.Value : new BsonString(next.Fence.Id.ToString("D")) },
            { "currentRefreshTokenId", next.CurrentRefreshTokenId is null ? BsonNull.Value : new BsonString(next.CurrentRefreshTokenId) },
            { "undelivered", next.Outbox.Any(entry => entry.State != OutboxState.Delivered) },
            { "payload", JsonSerializer.Serialize(next) }
        };
        var filter = current is null
            ? new BsonDocument { { "_id", next.GrantId }, { "payload", new BsonDocument("$exists", false) } }
            : new BsonDocument { { "_id", current.GrantId }, { "version", current.Version }, { "fenced", current.Fence is not null }, { "currentRefreshTokenId", current.CurrentRefreshTokenId is null ? BsonNull.Value : new BsonString(current.CurrentRefreshTokenId) } };
        if (next.Fence is { } fence)
        {
            filter.Add("cancelledReservations", new BsonDocument("$ne", fence.Id.ToString("D")));
        }

        if (replayEpoch is not null)
        {
            document.Add("replayEpoch", replayEpoch.Value);
        }

        try
        {
            // $set preserves all cancellation markers. Replacing the document could resurrect a delayed writer.
            var result = await _grants.UpdateOneAsync(filter, new BsonDocument("$set", document), new UpdateOptions { IsUpsert = current is null }, cancellationToken);
            return result.IsAcknowledged && (result.MatchedCount == 1 || result.UpsertedId is not null);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
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
