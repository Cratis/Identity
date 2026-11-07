// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Operational;

/// <summary>
/// Refuses to start while a legacy MongoDB account collection with documents exists, or cannot be ruled out.
/// Accounts live in Chronicle and are never imported, so starting over a populated legacy store would silently lose them.
/// The guard only reads and never changes MongoDB.
/// </summary>
/// <param name="options">The Identity options.</param>
/// <param name="probe">Looks at one MongoDB location.</param>
/// <param name="logger">The logger.</param>
/// <param name="defaultLocation">The historic default location; only specs override it.</param>
public sealed class LegacyAccountStoreGuard(
    IOptions<IdentityOptions> options,
    ILegacyAccountStoreProbe probe,
    ILogger<LegacyAccountStoreGuard> logger,
    MongoDBOptions? defaultLocation = null) : IHostedService
{
    /// <summary>The configuration key that confirms there is no legacy store.</summary>
    public const string OptOutSetting = "Identity:Accounts:ConfirmNoLegacyMongoStore";

    const string ConfiguredLocation = "configured";
    const string DefaultLocation = "default";

    readonly MongoDBOptions _default = defaultLocation ?? new() { ConnectionString = "mongodb://localhost:27017", Database = "cratis-identity" };

    /// <inheritdoc/>
    /// <exception cref="LegacyAccountStoreFound">A legacy account collection with documents exists.</exception>
    /// <exception cref="LegacyAccountStoreUnverifiable">A location could not be checked.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.Accounts.ConfirmNoLegacyMongoStore)
        {
            logger.CheckSkipped();
            return;
        }

        foreach (var (location, store) in Targets(settings.MongoDB))
        {
            var state = await Check(location, store, cancellationToken);
            if (state == LegacyStoreState.HasAccounts)
            {
                var found = new LegacyAccountStoreFound(location);
                logger.Refused(found.Message);
                throw found;
            }
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static bool SameLocation(MongoDBOptions first, MongoDBOptions second) =>
        string.Equals(first.ConnectionString, second.ConnectionString, StringComparison.Ordinal) &&
        string.Equals(first.Database, second.Database, StringComparison.Ordinal);

    IEnumerable<(string Location, MongoDBOptions Store)> Targets(MongoDBOptions configured)
    {
        yield return (ConfiguredLocation, configured);
        if (!SameLocation(configured, _default))
        {
            yield return (DefaultLocation, _default);
        }
    }

    async Task<LegacyStoreState> Check(string location, MongoDBOptions store, CancellationToken cancellationToken)
    {
        try
        {
            return await probe.Check(store.ConnectionString, store.Database, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var refused = new LegacyAccountStoreUnverifiable(location, exception is LegacyStoreProbeFailed failed ? failed.Reason : LegacyStoreFailure.Error);
            logger.RefusedUnverifiable(refused.Message, exception.GetType().Name);
            throw refused;
        }
    }
}
