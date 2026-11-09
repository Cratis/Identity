// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>Refuses a shared or unproven deployment boundary; configuration never grants backup independence.</summary>
public sealed class IndependentFenceLogConnection : IDisposable
{
    readonly MongoClient _client;
    readonly FenceLogOptions _options;
    readonly IMongoDatabase _operational;

    /// <summary>Constructs an independent connection without initializing custody.</summary>
    /// <param name="options">Independent provisioning and continuity evidence.</param>
    /// <param name="operational">The operational database identity probe.</param>
    /// <exception cref="RevocationPending">Configuration does not prove an independent store.</exception>
    public IndependentFenceLogConnection(FenceLogOptions options, IMongoDatabase operational)
    {
        _options = options;
        _operational = operational;
        RequireStorageOptions(options);
        RequireRestoreOptions(options);
        if (SameServers(operational, options) && options.Database == operational.DatabaseNamespace.DatabaseName)
        {
            throw new RevocationPending();
        }

        _client = new(options.ConnectionString);
        Database = _client.GetDatabase(options.Database);
    }

    /// <summary>Gets the configured independent database.</summary>
    public IMongoDatabase Database { get; }

    /// <inheritdoc/>
    public void Dispose() => _client.Dispose();

    /// <summary>Proves deployment separation and existing custody continuity.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Boundary verification completion.</returns>
    /// <exception cref="RevocationPending">The deployment or custody proof failed.</exception>
    public async Task Verify(CancellationToken cancellationToken)
    {
        var operational = await Deployment(_operational, cancellationToken);
        var independent = await Deployment(Database, cancellationToken);
        if (operational == independent)
        {
            throw new RevocationPending();
        }

        var log = new MongoIndependentFenceLog(Database, TimeProvider.System);
        var metadata = await log.Metadata(cancellationToken);
        if (metadata["instanceId"].AsString != _options.InstanceId || metadata["allocated"].AsInt64 < _options.MinimumSequence)
        {
            throw new RevocationPending();
        }

        _ = await log.Watermark(cancellationToken);

        await Database.GetCollection<BsonDocument>("fence-reservations").Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(new BsonDocument("sequence", 1), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
    }

    static bool SameServers(IMongoDatabase operational, FenceLogOptions options) =>
        operational.Client.Settings.Servers.Select(server => server.ToString()).Order(StringComparer.Ordinal)
            .SequenceEqual(MongoClientSettings.FromConnectionString(options.ConnectionString).Servers.Select(server => server.ToString()).Order(StringComparer.Ordinal), StringComparer.Ordinal);

    static void RequireStorageOptions(FenceLogOptions options)
    {
        var required = new[] { options.ConnectionString, options.Database, options.InstanceId };
        if (required.Any(string.IsNullOrWhiteSpace) || options.MinimumSequence is null or < 0)
        {
            throw new RevocationPending();
        }
    }

    static void RequireRestoreOptions(FenceLogOptions options)
    {
        if (!Enum.IsDefined(options.RestoreKind) || options.RestoreKind == RestoreKind.None != string.IsNullOrWhiteSpace(options.RestoreId))
        {
            throw new RevocationPending();
        }

        if (options.RestrictionsReestablished && options.RestoreKind == RestoreKind.None)
        {
            throw new RevocationPending();
        }
    }

    static async Task<string> Deployment(IMongoDatabase database, CancellationToken cancellationToken)
    {
        // Aliased hosts and different database names do not make separate restore boundaries.
        // Replica-set identity must be readable. Standalone or unverifiable deployments are refused.
        var configuration = await database.Client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
            new BsonDocument("replSetGetConfig", 1), cancellationToken: cancellationToken);
        return configuration["config"]["settings"]["replicaSetId"].AsObjectId.ToString();
    }
}
