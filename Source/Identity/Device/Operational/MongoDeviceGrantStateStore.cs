// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Device.Operational;

/// <summary>Compare-and-swap windows; Mongo availability is mandatory for admission.</summary>
/// <param name="database">The operational database.</param>
internal sealed class MongoDeviceGrantStateStore(IMongoDatabase database) : IDeviceGrantStateStore
{
    readonly IMongoCollection<DeviceGrantState> _states = database.GetCollection<DeviceGrantState>("device_grant_state")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<int> Increment(string id, DateTime now, TimeSpan window, CancellationToken cancellationToken)
    {
        var expired = new BsonDocument("$lte", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$ExpiresAt", DateTime.UnixEpoch }), now });
        var update = new BsonDocument("$set", new BsonDocument
        {
            { "Attempts", new BsonDocument("$cond", new BsonArray { expired, 1, new BsonDocument("$add", new BsonArray { "$Attempts", 1 }) }) },
            { "ExpiresAt", new BsonDocument("$cond", new BsonArray { expired, now.Add(window), "$ExpiresAt" }) }
        });
        var result = await _states.FindOneAndUpdateAsync(
            _ => _.Id == id,
            new PipelineUpdateDefinition<DeviceGrantState>(new[] { update }),
            new FindOneAndUpdateOptions<DeviceGrantState> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
            cancellationToken);
        return result.Attempts;
    }

    /// <inheritdoc/>
    public async Task<DeviceGrantState?> Read(string id, CancellationToken cancellationToken) =>
        await _states.Find(_ => _.Id == id).SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> Replace(DeviceGrantState state, long? revision, CancellationToken cancellationToken)
    {
        if (revision is null)
        {
            try
            {
                await _states.InsertOneAsync(state, cancellationToken: cancellationToken);
                return true;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        }

        var result = await _states.ReplaceOneAsync(_ => _.Id == state.Id && _.Revision == revision, state, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }
}
