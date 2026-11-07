// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_GrantFences;

/// <summary>Explicit test provisioning; sharing a test server is not a B1 deployment-independence test.</summary>
internal static class FenceLogDatabase
{
    internal static async Task<MongoIndependentFenceLog> Create(IMongoDatabase operational)
    {
        var database = operational.Client.GetDatabase($"fence-log-test-{Guid.NewGuid():N}");
        await database.GetCollection<BsonDocument>("fence-custody").InsertOneAsync(new BsonDocument
        {
            { "_id", "metadata" }, { "instanceId", Guid.NewGuid().ToString("D") }, { "allocated", 0L }, { "epoch", 0L }
        });
        return new(database, TimeProvider.System);
    }
}
