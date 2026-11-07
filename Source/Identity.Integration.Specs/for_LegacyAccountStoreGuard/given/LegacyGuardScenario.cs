// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Operational;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_LegacyAccountStoreGuard.given;

/// <summary>Runs the real guard and the real probe against the host's MongoDB container, using databases of its own.</summary>
/// <param name="host">The host that owns the container.</param>
public sealed class LegacyGuardScenario(IdentityHost host)
{
    public string ConfiguredDatabase { get; } = $"legacy-configured-{Guid.NewGuid():N}";

    public string DefaultDatabase { get; } = $"legacy-default-{Guid.NewGuid():N}";

    public string ConnectionString => host.MongoConnectionString;

    public IMongoDatabase Configured => new MongoClient(ConnectionString).GetDatabase(ConfiguredDatabase);

    public async Task<Exception?> Start(string connectionString, bool optOut = false)
    {
        var options = new IdentityOptions
        {
            MongoDB = new() { ConnectionString = connectionString, Database = ConfiguredDatabase },
            Accounts = new() { ConfirmNoLegacyMongoStore = optOut }
        };
        var guard = new LegacyAccountStoreGuard(
            Options.Create(options),
            new LegacyAccountStoreProbe(),
            NullLogger<LegacyAccountStoreGuard>.Instance,
            new() { ConnectionString = ConnectionString, Database = DefaultDatabase });
        try
        {
            await guard.StartAsync(TestContext.Current.CancellationToken);
            return null;
        }
        catch (Exception exception) when (exception is LegacyAccountStoreFound or LegacyAccountStoreUnverifiable)
        {
            return exception;
        }
    }

    public Task<Exception?> Start(bool optOut = false) => Start(ConnectionString, optOut);

    public Task InsertLegacyAccount(IMongoDatabase database) =>
        database.GetCollection<BsonDocument>("accounts").InsertOneAsync(new BsonDocument { ["github_id"] = 1L }, cancellationToken: TestContext.Current.CancellationToken);

    public async Task<List<string>> CollectionNames(IMongoDatabase database) =>
        await (await database.ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
}
