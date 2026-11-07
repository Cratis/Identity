// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Cratis.Identity.Integration.for_GrantFences;

/// <summary>Explicit native test provisioning on the host's separate custody deployment; not production B1 evidence.</summary>
internal static class FenceLogDatabase
{
    /// <summary>A throwaway operational database, so spec fences never reach the host's own outbox worker.</summary>
    /// <param name="host">The spec host.</param>
    /// <returns>A new, empty database.</returns>
    internal static MongoDB.Driver.IMongoDatabase Operational(IdentityHost host) =>
        host.Database.Client.GetDatabase($"fence-store-test-{Guid.NewGuid():N}");

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
