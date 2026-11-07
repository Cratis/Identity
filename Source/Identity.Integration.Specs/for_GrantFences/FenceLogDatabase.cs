// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Cratis.Identity.Integration.for_GrantFences;

/// <summary>Explicit native test provisioning on the host's separate custody deployment; not production B1 evidence.</summary>
internal static class FenceLogDatabase
{
    internal static async Task<MongoIndependentFenceLog> Create(IdentityHost host)
    {
        var independent = host.Services.GetRequiredService<IndependentFenceLogConnection>().Database;
        var database = independent.Client.GetDatabase($"fence-log-test-{Guid.NewGuid():N}");
        await database.GetCollection<BsonDocument>("fence-custody").InsertOneAsync(new BsonDocument
        {
            { "_id", "metadata" }, { "instanceId", Guid.NewGuid().ToString("D") }, { "allocated", 0L }, { "epoch", 0L }
        });
        return new(database, TimeProvider.System);
    }
}
