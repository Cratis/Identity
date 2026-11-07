// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Operational;

/// <summary>
/// Reads one MongoDB location to see whether the legacy accounts collection holds documents.
/// It uses a short-lived client with bounded timeouts, lists one collection name and reads at most one document id.
/// It never writes, creates, drops or reads account contents.
/// </summary>
public sealed class LegacyAccountStoreProbe : ILegacyAccountStoreProbe
{
    const string CollectionName = "accounts";
    static readonly TimeSpan _driverTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _overallTimeout = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public async Task<LegacyStoreState> Check(string connectionString, string database, CancellationToken cancellationToken)
    {
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overall.CancelAfter(_overallTimeout);
        try
        {
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = _driverTimeout;
            settings.ConnectTimeout = _driverTimeout;
            settings.SocketTimeout = _driverTimeout;
            using var client = new MongoClient(settings);
            var db = client.GetDatabase(database);

            using var names = await db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", CollectionName) }, overall.Token);
            if (!await names.AnyAsync(overall.Token))
            {
                return LegacyStoreState.Absent;
            }

            var any = await db.GetCollection<BsonDocument>(CollectionName)
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Project(Builders<BsonDocument>.Projection.Include("_id"))
                .Limit(1)
                .AnyAsync(overall.Token);
            return any ? LegacyStoreState.HasAccounts : LegacyStoreState.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new LegacyStoreProbeFailed(Classify(exception), exception);
        }
    }

    static LegacyStoreFailure Classify(Exception exception) => exception switch
    {
        OperationCanceledException or TimeoutException => LegacyStoreFailure.Timeout,
        MongoAuthenticationException or MongoCommandException { Code: 13 or 18 } => LegacyStoreFailure.AuthenticationFailed,
        MongoException => LegacyStoreFailure.Unreachable,
        _ => LegacyStoreFailure.Error
    };
}
