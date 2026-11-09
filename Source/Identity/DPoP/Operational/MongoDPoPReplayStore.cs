// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.DPoP.Operational;

/// <summary>Majority-durable, cross-replica proof reservations. TTL cleanup is not admission.</summary>
/// <param name="database">The operational database.</param>
/// <param name="time">The clock used for conditional expired-entry replacement.</param>
internal sealed class MongoDPoPReplayStore(IMongoDatabase database, TimeProvider time) : IDPoPReplayStore
{
    /// <summary>The operational-only proof reservation collection.</summary>
    internal const string CollectionName = "dpop_proof_replay";
    readonly IMongoCollection<BsonDocument> _entries = database.GetCollection<BsonDocument>(CollectionName).WithWriteConcern(WriteConcern.WMajority).WithReadPreference(ReadPreference.Primary);

    /// <inheritdoc/>
    public async Task<bool> TryReserve(string domain, string origin, string jkt, string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var id = Key(domain, origin, jkt, jti);
        var entry = new BsonDocument { ["_id"] = id, ["ExpiresAt"] = expiresAt.UtcDateTime };
        try
        {
            await _entries.InsertOneAsync(entry, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id) & Builders<BsonDocument>.Filter.Lte("ExpiresAt", time.GetUtcNow().UtcDateTime);
            return (await _entries.ReplaceOneAsync(filter, entry, cancellationToken: cancellationToken)).ModifiedCount == 1;
        }
    }

    /// <summary>Hashes unambiguously framed scope fields, without persisting raw proof identifiers.</summary>
    /// <param name="fields">The domain, origin, key and proof ID.</param>
    /// <returns>The scoped digest.</returns>
    internal static string Key(params string[] fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var field in fields)
        {
            var bytes = Encoding.UTF8.GetBytes(field);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
