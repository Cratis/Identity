// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_TokenStorage;

[Collection(IdentityCollection.Name)]
public class when_reading_the_token_indexes(IdentityHost host) : IAsyncLifetime
{
    List<BsonDocument> _indexes;

    public async ValueTask InitializeAsync() =>
        _indexes = await (await host.Database.GetCollection<BsonDocument>("openiddict.tokens").Indexes.ListAsync()).ToListAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_expire_entries_at_their_expiration_date() =>
        _indexes.Exists(_ => _.Contains("expireAfterSeconds") && _["expireAfterSeconds"].ToInt32() == 0 && _["key"].AsBsonDocument.Contains("expiration_date")).ShouldBeTrue();
}
