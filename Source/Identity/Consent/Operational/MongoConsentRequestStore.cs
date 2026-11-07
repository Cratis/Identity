// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.ConsentFlow.Operational;

/// <summary>Majority-durable, single-document consent transitions, separate from OpenIddict's collections.</summary>
/// <param name="database">The operational database, created through MongoClientFactory.</param>
public sealed class MongoConsentRequestStore(IMongoDatabase database) : IConsentRequestStore
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
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("PurgeAt"), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero })
        ],
        cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> Create(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (request is not { State: ConsentRequestState.Pending, Revision: 0, AuthorizationId: null, CodeClaimed: false, PurgeAt: null }) return false;
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
    public async Task<bool> CompareExchange(ConsentRequest expected, ConsentRequest replacement, CancellationToken cancellationToken)
    {
        if (!ConsentRequestTransitions.CanReplace(expected, replacement)) return false;
        var filter = Builders<BsonDocument>.Filter;
        var result = await _requests.ReplaceOneAsync(
            filter.Eq("_id", expected.Id.Value.ToString("D")) & filter.Eq("Revision", expected.Revision) & filter.Eq("State", expected.State.ToString()),
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

    static BsonDocument ToDocument(ConsentRequest request)
    {
        var binding = request.Binding;
        var document = new BsonDocument
        {
            ["_id"] = request.Id.Value.ToString("D"),
            ["Realm"] = binding.Realm,
            ["Account"] = binding.Account.Value,
            ["Client"] = binding.Client,
            ["Tenant"] = binding.Tenant,
            ["Resource"] = binding.Resource,
            ["Scopes"] = binding.Scopes,
            ["Session"] = binding.Session,
            ["Deadline"] = new BsonDateTime(request.Deadline.UtcDateTime),
            ["State"] = request.State.ToString(),
            ["Revision"] = request.Revision,
            ["CodeClaimed"] = request.CodeClaimed,
            ["AuthorizationId"] = request.AuthorizationId is null ? BsonNull.Value : new BsonString(request.AuthorizationId)
        };
        if (request.PurgeAt is { } purgeAt) document["PurgeAt"] = new BsonDateTime(purgeAt.UtcDateTime);
        return document;
    }

    static ConsentRequest FromDocument(BsonDocument document) => new(
        new(Guid.Parse(document["_id"].AsString)),
        new(document["Realm"].AsString, new AccountSubject(document["Account"].AsString), document["Client"].AsString, document["Tenant"].AsString, document["Resource"].AsString, document["Scopes"].AsString, document["Session"].AsString),
        new DateTimeOffset(document["Deadline"].ToUniversalTime()),
        Enum.Parse<ConsentRequestState>(document["State"].AsString),
        document["Revision"].AsInt64,
        document["AuthorizationId"].IsBsonNull ? null : document["AuthorizationId"].AsString,
        document["CodeClaimed"].AsBoolean,
        document.TryGetValue("PurgeAt", out var purgeAt) ? new DateTimeOffset(purgeAt.ToUniversalTime()) : null);
}
