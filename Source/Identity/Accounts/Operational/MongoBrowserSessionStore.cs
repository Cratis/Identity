// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Accounts.Operational;

/// <summary>
/// Majority-durable <see cref="IBrowserSessionStore"/>. Records expire with the session cookie lifetime.
/// </summary>
/// <param name="database">The operational database.</param>
/// <param name="options">The <see cref="IdentityOptions"/> that set the session lifetime.</param>
/// <param name="time">The clock.</param>
internal sealed class MongoBrowserSessionStore(IMongoDatabase database, IOptions<IdentityOptions> options, TimeProvider time) : IBrowserSessionStore
{
    /// <summary>The operational-only session collection.</summary>
    internal const string CollectionName = "browser_sessions";

    readonly IMongoCollection<BsonDocument> _sessions = database.GetCollection<BsonDocument>(CollectionName).WithWriteConcern(WriteConcern.WMajority).WithReadPreference(ReadPreference.Primary);

    /// <inheritdoc/>
    public Task Record(BrowserSession session, CancellationToken cancellationToken) =>
        _sessions.InsertOneAsync(
            new BsonDocument
            {
                ["_id"] = session.SessionId,
                ["Subject"] = session.Subject,
                ["LinkId"] = new BsonBinaryData(session.LinkId, GuidRepresentation.Standard),
                ["AuthTime"] = session.AuthTime.UtcDateTime,
                ["ExpiresAt"] = (time.GetUtcNow() + options.Value.SessionLifetime).UtcDateTime
            },
            cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public async Task<BrowserSession?> Find(string sessionId, CancellationToken cancellationToken)
    {
        var document = await _sessions.Find(Builders<BsonDocument>.Filter.Eq("_id", sessionId)).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        return new BrowserSession(
            document["_id"].AsString,
            document["Subject"].AsString,
            document["LinkId"].AsGuid,
            new DateTimeOffset(DateTime.SpecifyKind(document["AuthTime"].ToUniversalTime(), DateTimeKind.Utc)),
            document.TryGetValue("RevokedAt", out var revoked) && revoked.IsValidDateTime ? new DateTimeOffset(DateTime.SpecifyKind(revoked.ToUniversalTime(), DateTimeKind.Utc)) : null);
    }

    /// <summary>Ensures the expiry index exists.</summary>
    /// <param name="database">The operational database.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal static Task EnsureIndexes(IMongoDatabase database, CancellationToken cancellationToken) =>
        database.GetCollection<BsonDocument>(CollectionName).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("ExpiresAt"), new CreateIndexOptions { Name = "browser_session_expiry", ExpireAfter = TimeSpan.Zero }),
            cancellationToken: cancellationToken);
}
