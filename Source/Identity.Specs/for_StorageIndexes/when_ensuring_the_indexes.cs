// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.for_StorageIndexes;

public class when_ensuring_the_indexes : Specification
{
    IMongoDatabase _database;
    IMongoIndexManager<OpenIddictMongoDbToken> _tokenIndexes;
    List<CreateIndexModel<OpenIddictMongoDbToken>> _models;

    void Establish()
    {
        var options = new OpenIddictMongoDbOptions();
        var monitor = Substitute.For<IOptionsMonitor<OpenIddictMongoDbOptions>>();
        monitor.CurrentValue.Returns(options);

        _models = [];
        _tokenIndexes = Substitute.For<IMongoIndexManager<OpenIddictMongoDbToken>>();
        _tokenIndexes.CreateManyAsync(Arg.Do<IEnumerable<CreateIndexModel<OpenIddictMongoDbToken>>>(_ => _models.AddRange(_)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));
        var tokens = Substitute.For<IMongoCollection<OpenIddictMongoDbToken>>();
        tokens.Indexes.Returns(_tokenIndexes);

        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(tokens);
        _indexes = new StorageIndexes(_database, monitor);
    }

    StorageIndexes _indexes;

    async Task Because() => await _indexes.Ensure(CancellationToken.None);

    CreateIndexModel<OpenIddictMongoDbToken> Ttl => _models.Single(_ => _.Options?.ExpireAfter is not null);

    [Fact] void should_not_request_an_operational_accounts_collection() => _database.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IMongoDatabase.GetCollection) && Equals(call.GetArguments()[0], "accounts")).ShouldBeFalse();
    [Fact] void should_not_request_application_or_scope_collections() => _database.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(IMongoDatabase.GetCollection)).Select(call => (string)call.GetArguments()[0]).Order().ShouldEqual(new[] { "consent-requests", "grant-fences", "grant-fence-log", new OpenIddictMongoDbOptions().AuthorizationsCollectionName, new OpenIddictMongoDbOptions().TokensCollectionName }.Order());
    [Fact] void should_create_one_ttl_index() => _models.Count(_ => _.Options?.ExpireAfter is not null).ShouldEqual(1);
    [Fact] void should_expire_entries_at_their_expiration_date() => Ttl.Options.ExpireAfter.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_index_the_expiration_date() => Ttl.Keys.Render(new RenderArgs<OpenIddictMongoDbToken>(BsonSerializer.LookupSerializer<OpenIddictMongoDbToken>(), BsonSerializer.SerializerRegistry)).ToString().ShouldContain("expiration_date");
}
