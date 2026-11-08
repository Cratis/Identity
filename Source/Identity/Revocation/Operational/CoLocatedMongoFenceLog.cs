// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>INTERIM ONLY: majority-durable and idempotent, but co-located with operational storage.</summary>
/// <remarks>This adapter does NOT meet B1 or provide restore independence. The independent log and restore gate remain #42.</remarks>
/// <param name="database">The same operational database; intentionally not claimed to be independent.</param>
internal sealed class CoLocatedMongoFenceLog(IMongoDatabase database) : IFenceLog
{
    readonly IMongoCollection<BsonDocument> _log = database.GetCollection<BsonDocument>("grant-fence-log")
        .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<long> Append(GrantFence fence, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(fence);
        try
        {
            await _log.InsertOneAsync(new BsonDocument { { "_id", fence.Id.ToString("D") }, { "sequence", fence.Sequence }, { "payload", payload } }, cancellationToken: cancellationToken);
            return fence.Sequence;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await _log.Find(new BsonDocument("_id", fence.Id.ToString("D"))).FirstOrDefaultAsync(cancellationToken);
            if (existing is null || existing["sequence"].AsInt64 != fence.Sequence || existing["payload"].AsString != payload)
            {
                throw new FenceLogConflict();
            }

            return fence.Sequence;
        }
    }
}
