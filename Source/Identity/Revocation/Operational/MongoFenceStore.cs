// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>One majority-durable document is the refresh/revoke ordering point, not an event projection.</summary>
/// <param name="database">The operational database.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class MongoFenceStore(IMongoDatabase database, TimeProvider timeProvider) : IFenceStore
{
    const int Attempts = 8;
    readonly IMongoCollection<BsonDocument> _grants = database.GetCollection<BsonDocument>("grant-fences")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);
    readonly IMongoCollection<BsonDocument> _log = database.GetCollection<BsonDocument>("grant-fence-log")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<GrantState?> Read(string grantId, CancellationToken cancellationToken)
    {
        var document = await _grants.Find(new BsonDocument("_id", grantId)).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
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

            var sequence = await _log.FindOneAndUpdateAsync(
                new BsonDocument("_id", "sequence"),
                new BsonDocument("$inc", new BsonDocument("value", 1L)),
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                cancellationToken);
            var fence = new GrantFence(Guid.NewGuid(), sequence["value"].AsInt64, grantId, subject, reason, timeProvider.GetUtcNow());
            var entries = new List<OutboxEntry>(current?.Outbox ?? [])
            {
                OutboxEntry.Create(FactKind.AuthorizationRevocation, grantId, subject, fence.RecordedAt, fence: fence),
                OutboxEntry.Create(FactKind.FamilyRevocation, grantId, subject, fence.RecordedAt, fence: fence)
            };
            if (reason == RevocationReason.RefreshTokenReused)
            {
                entries.Add(OutboxEntry.Create(FactKind.Reuse, grantId, subject, fence.RecordedAt, fence: fence));
            }

            var next = new GrantState(grantId, (current?.Version ?? 0) + 1, null, fence, [.. entries]);
            if (await Replace(current, next, cancellationToken))
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

        var entry = OutboxEntry.Create(FactKind.FamilyStarted, grantId, subject, startedAt, absoluteExpiresAt);
        var state = new GrantState(grantId, 1, tokenId, null, [entry]);
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

        if (current.Fence is not null)
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

    async Task<bool> Replace(GrantState? current, GrantState next, CancellationToken cancellationToken)
    {
        var document = new BsonDocument
        {
            { "_id", next.GrantId }, { "version", next.Version }, { "fenced", next.Fence is not null },
            { "currentRefreshTokenId", next.CurrentRefreshTokenId is null ? BsonNull.Value : new BsonString(next.CurrentRefreshTokenId) },
            { "undelivered", next.Outbox.Any(entry => entry.State != OutboxState.Delivered) },
            { "payload", JsonSerializer.Serialize(next) }
        };
        if (current is null)
        {
            try
            {
                await _grants.InsertOneAsync(document, cancellationToken: cancellationToken);
                return true;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        var result = await _grants.ReplaceOneAsync(new BsonDocument { { "_id", current.GrantId }, { "version", current.Version }, { "fenced", current.Fence is not null }, { "currentRefreshTokenId", current.CurrentRefreshTokenId is null ? BsonNull.Value : new BsonString(current.CurrentRefreshTokenId) } }, document, cancellationToken: cancellationToken);
        return result.IsAcknowledged && result.MatchedCount == 1;
    }
}
