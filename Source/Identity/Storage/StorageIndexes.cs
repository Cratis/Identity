// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Operational;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Storage;

/// <summary>
/// Creates the MongoDB indexes Identity and OpenIddict rely on. OpenIddict.MongoDb does not create indexes itself.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/>.</param>
/// <param name="openIddict">The <see cref="OpenIddictMongoDbOptions"/>.</param>
internal class StorageIndexes(IMongoDatabase database, IOptionsMonitor<OpenIddictMongoDbOptions> openIddict)
{
    /// <summary>
    /// Ensures all indexes exist.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Ensure(CancellationToken cancellationToken)
    {
        var options = openIddict.CurrentValue;
        await MongoConsentRequestStore.EnsureIndexes(database, cancellationToken);
        await EnsureGrantFences(cancellationToken);

        await database.GetCollection<OpenIddictMongoDbAuthorization>(options.AuthorizationsCollectionName).Indexes.CreateOneAsync(
            new CreateIndexModel<OpenIddictMongoDbAuthorization>(
                Builders<OpenIddictMongoDbAuthorization>.IndexKeys
                    .Ascending(_ => _.ApplicationId)
                    .Ascending(_ => _.Scopes)
                    .Ascending(_ => _.Status)
                    .Ascending(_ => _.Subject)
                    .Ascending(_ => _.Type)),
            cancellationToken: cancellationToken);

        await database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName).Indexes.CreateManyAsync(
            [
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(_ => _.ReferenceId),
                    new CreateIndexOptions<OpenIddictMongoDbToken>
                    {
                        Unique = true,
                        PartialFilterExpression = Builders<OpenIddictMongoDbToken>.Filter.Type(_ => _.ReferenceId, BsonType.String)
                    }),
                new CreateIndexModel<OpenIddictMongoDbToken>(Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(_ => _.AuthorizationId)),
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys
                        .Ascending(_ => _.ApplicationId)
                        .Ascending(_ => _.Status)
                        .Ascending(_ => _.Subject)
                        .Ascending(_ => _.Type)),

                // Authorization codes, refresh-token rotations and any other short-lived entry leave the store when they
                // expire. The TTL monitor lags by up to about a minute, so expiry is also enforced at redemption.
                new CreateIndexModel<OpenIddictMongoDbToken>(
                    Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(_ => _.ExpirationDate),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.Zero })
            ],
            cancellationToken);
    }

    async Task EnsureGrantFences(CancellationToken cancellationToken)
    {
        // Mongo's unique _id is the grant/fence identity. Tombstones and outbox intents never receive TTLs.
        await database.GetCollection<BsonDocument>("grant-fences").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("undelivered"), new CreateIndexOptions { Name = "undelivered_grant_facts" }), cancellationToken: cancellationToken);
        await database.GetCollection<BsonDocument>("grant-fence-log").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("sequence"), new CreateIndexOptions { Name = "unique_fence_sequence", Unique = true, Sparse = true }), cancellationToken: cancellationToken);
    }
}
