// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Represents an implementation of <see cref="IAccounts"/> backed by MongoDB.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class Accounts(IMongoDatabase database, TimeProvider timeProvider) : IAccounts
{
    /// <summary>
    /// The name of the collection accounts are stored in.
    /// </summary>
    public const string CollectionName = "accounts";

    readonly IMongoCollection<AccountDocument> _collection = database.GetCollection<AccountDocument>(CollectionName);

    /// <inheritdoc/>
    public async Task<Account> RecordGitHubSignIn(GitHubUser user, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var update = Builders<AccountDocument>.Update
            .SetOnInsert(_ => _.Id, Guid.NewGuid().ToString())
            .SetOnInsert(_ => _.CreatedAt, now)
            .Set(_ => _.GitHubLogin, user.Login)
            .Set(_ => _.Name, user.Name)
            .Set(_ => _.LastSignedInAt, now);

        // The unique index on github_id makes the upsert atomic: a concurrent first sign-in either inserts
        // or is retried by the server as an update of the document the other one inserted.
        var document = await _collection.FindOneAndUpdateAsync(
            _ => _.GitHubId == user.Id,
            update,
            new FindOneAndUpdateOptions<AccountDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return document.ToAccount();
    }

    /// <inheritdoc/>
    public async Task<Account?> FindById(string id, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(_ => _.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToAccount();
    }
}
