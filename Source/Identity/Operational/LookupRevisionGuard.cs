// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using MongoDB.Driver;

namespace Cratis.Identity.Operational;

/// <summary>Fails closed if a revision is changed, removed or reused with different key material.</summary>
/// <param name="collection">The durable operational metadata collection.</param>
/// <param name="keys">The configured key.</param>
internal class LookupRevisionGuard(IMongoCollection<LookupRevision> collection, IUpstreamLookupKeys keys) : IHostedService
{
    /// <inheritdoc/>
    /// <exception cref="LookupKeyRevisionMismatch">The durable revision does not match the configured key.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var key = keys.Current;
        var existing = await collection.Find(FilterDefinition<LookupRevision>.Empty).ToListAsync(cancellationToken);
        if (existing.Exists(row => row.RetainedRevision != key.Revision || row.Check != key.Check))
        {
            throw new LookupKeyRevisionMismatch();
        }

        var stored = existing.SingleOrDefault(row => row.Id == LookupRevision.SingletonId);
        if (stored is null)
        {
            try
            {
                await collection.InsertOneAsync(new(LookupRevision.SingletonId, key.Check, key.Revision), cancellationToken: cancellationToken);
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Another instance claimed the singleton. Its retained key must still match ours.
            }

            stored = await collection.Find(row => row.Id == LookupRevision.SingletonId).SingleOrDefaultAsync(cancellationToken);
        }

        if (stored is null || stored.Revision != key.Revision || stored.Check != key.Check)
        {
            throw new LookupKeyRevisionMismatch();
        }

        _ = await collection.FindOneAndUpdateAsync(
            row => row.Id == LookupRevision.SingletonId && row.Revision == key.Revision && row.Check == key.Check,
            Builders<LookupRevision>.Update.Set(row => row.Revision, key.Revision).Set(row => row.Check, key.Check),
            new FindOneAndUpdateOptions<LookupRevision> { ReturnDocument = ReturnDocument.After },
            cancellationToken) ?? throw new LookupKeyRevisionMismatch();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
