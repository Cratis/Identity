// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Durable key-check metadata, separate from event payloads and never secret material.</summary>
/// <param name="Id">The one retained revision.</param>
/// <param name="Check">The domain-separated key check.</param>
public record LookupRevision(int Id, string Check);

/// <summary>Fails closed if a revision is changed, removed or reused with different key material.</summary>
/// <param name="collection">The development metadata collection.</param>
/// <param name="keys">The configured key.</param>
public class LookupRevisionGuard(IMongoCollection<LookupRevision> collection, IUpstreamLookupKeys keys) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var key = keys.Current;
        var existing = await collection.Find(FilterDefinition<LookupRevision>.Empty).ToListAsync(cancellationToken);
        if (existing.Any(row => row.Id != key.Revision || row.Check != key.Check))
        {
            throw new AccountOperationRefused("lookup_key_revision_mismatch");
        }
        await collection.UpdateOneAsync(row => row.Id == key.Revision,
            Builders<LookupRevision>.Update.SetOnInsert(row => row.Check, key.Check),
            new UpdateOptions { IsUpsert = true }, cancellationToken);
        var stored = await collection.Find(row => row.Id == key.Revision).SingleAsync(cancellationToken);
        if (stored.Check != key.Check)
        {
            throw new AccountOperationRefused("lookup_key_revision_mismatch");
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
