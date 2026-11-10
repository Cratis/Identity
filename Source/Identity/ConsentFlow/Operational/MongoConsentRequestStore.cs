// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;
using Cratis.Identity.Sessions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.ConsentFlow.Operational;

/// <summary>Majority-durable, single-document consent transitions, separate from OpenIddict's collections.</summary>
/// <param name="database">The operational database, created through MongoClientFactory.</param>
internal sealed class MongoConsentRequestStore(IMongoDatabase database) : IConsentRequestStore
{
    /// <summary>The operational collection; the Mongo _id index uniquely enforces ConsentRequestId.</summary>
    public const string CollectionName = "consent-requests";

    readonly IMongoCollection<BsonDocument> _requests = database.GetCollection<BsonDocument>(CollectionName)
        .WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority).WithReadPreference(ReadPreference.Primary);

    /// <summary>Creates the reconciliation and safe-terminal TTL indexes.</summary>
    /// <param name="database">The operational database.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The index creation task.</returns>
    public static Task EnsureIndexes(IMongoDatabase database, CancellationToken cancellationToken) =>
        database.GetCollection<BsonDocument>(CollectionName).Indexes.CreateManyAsync(
        [
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("State").Ascending("_id")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("Account").Ascending("Session").Ascending("Realm").Ascending("State").Descending("CreatedAt").Ascending("Deadline")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("PurgeAt"), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero })
        ],
        cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> Create(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (request is not { State: ConsentRequestState.Pending, Revision: 0, AuthorizationId: null, CodeClaimed: false, PurgeAt: null, Selection: null }) return false;
        try
        {
            await _requests.InsertOneAsync(ToDocument(request), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ConsentRequest?> Read(ConsentRequestId id, CancellationToken cancellationToken)
    {
        var document = await _requests.Find(Builders<BsonDocument>.Filter.Eq("_id", id.Value.ToString("D"))).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : FromDocument(document);
    }

    /// <inheritdoc/>
    public async Task<ConsentRequest?> FindPending(AccountSubject account, SessionId session, RealmId realm, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var condition = filter.Eq("Account", account.Value) & filter.Eq("Session", session.Value) & filter.Eq("Realm", realm.Value) &
            filter.Eq("State", nameof(ConsentRequestState.Pending)) & filter.Gt("Deadline", new BsonDateTime(now.UtcDateTime)) &
            new BsonDocument("$expr", new BsonDocument("$gt", new BsonArray { "$Deadline", "$$NOW" }));
        var document = await _requests.Find(condition).Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt")).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : FromDocument(document);
    }

    /// <inheritdoc/>
    public async Task<bool> CompareExchange(ConsentRequest expected, ConsentRequest replacement, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!ConsentRequestTransitions.CanReplace(expected, replacement)) return false;
        var filter = Builders<BsonDocument>.Filter;
        var condition = filter.Eq("_id", expected.Id.Value.ToString("D")) & filter.Eq("Revision", expected.Revision) & filter.Eq("State", expected.State.ToString());
        condition &= expected.RecoveryEpoch == 0
            ? filter.Eq("RecoveryEpoch", 0L) | filter.Exists("RecoveryEpoch", false)
            : filter.Eq("RecoveryEpoch", expected.RecoveryEpoch);
        if (RequiresDeadline(expected.State, replacement.State))
        {
            condition &= filter.Gt("Deadline", new BsonDateTime(now.UtcDateTime)) &
                new BsonDocument("$expr", new BsonDocument("$gt", new BsonArray { "$Deadline", "$$NOW" }));
        }

        var result = await _requests.ReplaceOneAsync(
            condition,
            ToDocument(replacement),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConsentRequest>> Unfinished(int limit, ConsentRequestId? after, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        var filter = Builders<BsonDocument>.Filter;
        var unfinished = filter.In("State", [nameof(ConsentRequestState.Pending), nameof(ConsentRequestState.Decided), nameof(ConsentRequestState.EvidenceRecorded)]);
        if (after is not null) unfinished &= filter.Gt("_id", after.Value.ToString("D"));
        var rows = await _requests.Find(unfinished).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(limit).ToListAsync(cancellationToken);
        return rows.Select(FromDocument).ToArray();
    }

    static bool RequiresDeadline(ConsentRequestState before, ConsentRequestState after) =>
        after == ConsentRequestState.Active || (before == ConsentRequestState.Pending && after is ConsentRequestState.Decided or ConsentRequestState.Denied);

    static BsonDocument ToDocument(ConsentRequest request)
    {
        var binding = request.Binding;
        var document = new BsonDocument
        {
            ["_id"] = request.Id.Value.ToString("D"),
            ["Realm"] = binding.Realm.Value,
            ["Account"] = binding.Account.Value,
            ["Client"] = binding.Client.Value,
            ["Tenant"] = binding.Tenant.Value,
            ["Resource"] = binding.Resource,
            ["Scopes"] = binding.Scopes.Value,
            ["Session"] = binding.Session.Value,
            ["AuthorizationRequest"] = binding.AuthorizationRequest.Value.ToString("D"),
            ["Deadline"] = new BsonDateTime(request.Deadline.UtcDateTime),
            ["CreatedAt"] = new BsonDateTime(request.CreatedAt.UtcDateTime),
            ["State"] = request.State.ToString(),
            ["Revision"] = request.Revision,
            ["RecoveryEpoch"] = request.RecoveryEpoch,
            ["CodeClaimed"] = request.CodeClaimed,
            ["AuthorizationId"] = request.AuthorizationId is null ? BsonNull.Value : new BsonString(request.AuthorizationId)
        };
        if (request.Selection is { } selection) document["Selection"] = new BsonDocument { ["Tenant"] = selection.Tenant.Value, ["Scopes"] = selection.Scopes.Value };
        if (request.PurgeAt is { } purgeAt) document["PurgeAt"] = new BsonDateTime(purgeAt.UtcDateTime);
        return document;
    }

    static ConsentRequest FromDocument(BsonDocument document) => new(
        new(Guid.Parse(document["_id"].AsString)),
        new(document["Realm"].AsString, new AccountSubject(document["Account"].AsString), document["Client"].AsString, document["Tenant"].AsString, document["Resource"].AsString, document["Scopes"].AsString, document["Session"].AsString, new AuthorizationRequestId(Guid.Parse(document["AuthorizationRequest"].AsString))),
        new DateTimeOffset(document["Deadline"].ToUniversalTime()),
        Enum.Parse<ConsentRequestState>(document["State"].AsString),
        document["Revision"].AsInt64,
        document["AuthorizationId"].IsBsonNull ? null : document["AuthorizationId"].AsString,
        document["CodeClaimed"].AsBoolean,
        document.TryGetValue("PurgeAt", out var purgeAt) ? new DateTimeOffset(purgeAt.ToUniversalTime()) : null,
        document.TryGetValue("CreatedAt", out var createdAt) ? new DateTimeOffset(createdAt.ToUniversalTime()) : default,
        document.TryGetValue("Selection", out var selection) ? new(selection["Tenant"].AsString, selection["Scopes"].AsString) : null,
        document.TryGetValue("RecoveryEpoch", out var epoch) ? epoch.AsInt64 : 0);
}
