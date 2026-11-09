// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Operational;

/// <summary>
/// Remembers subject tokens across replicas and restarts using a majority-durable atomic upsert.
/// </summary>
/// <param name="database">The operational MongoDB database.</param>
/// <param name="time">The clock used to enforce expiry despite TTL-monitor lag.</param>
internal sealed class MongoSubjectTokenReplayGuard(IMongoDatabase database, TimeProvider time) : ISubjectTokenReplayGuard
{
    /// <summary>
    /// The operational replay collection.
    /// </summary>
    internal const string CollectionName = "subject-token-replays";

    /// <summary>
    /// The soft capacity limit; expired entries count until the TTL monitor removes them.
    /// </summary>
    internal const int MaximumEntries = 1_000_000;

    /// <summary>
    /// The deadline for both the capacity check and the atomic write.
    /// </summary>
    internal static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>(CollectionName)
        .WithWriteConcern(WriteConcern.WMajority)
        .WithReadConcern(ReadConcern.Majority)
        .WithReadPreference(ReadPreference.Primary);

    /// <inheritdoc/>
    public async Task<ReplayRecordResult> Record(string key, DateTimeOffset until, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OperationTimeout);
        try
        {
            if (await _collection.EstimatedDocumentCountAsync(cancellationToken: timeout.Token) >= MaximumEntries)
            {
                return ReplayRecordResult.Full;
            }

            // A live _id misses this filter, so upsert collides with its unique index. An expired leftover is
            // replaced in the same atomic operation: there is no read/modify/write race or TTL-monitor dependency.
            var filter = Builders<BsonDocument>.Filter.Eq("_id", key) & Builders<BsonDocument>.Filter.Lte("Until", time.GetUtcNow().UtcDateTime);
            await _collection.FindOneAndUpdateAsync(
                filter,
                Builders<BsonDocument>.Update.Set("Until", until.UtcDateTime),
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true },
                timeout.Token);
            return ReplayRecordResult.Recorded;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return ReplayRecordResult.AlreadyUsed;
        }
        catch (MongoCommandException exception) when (exception.Code is 11000 or 11001 or 12582)
        {
            return ReplayRecordResult.AlreadyUsed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReplayRecordResult.Unavailable;
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            return ReplayRecordResult.Unavailable;
        }
    }
}
