// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Operational;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_querying_the_operational_store : Deciding.given.an_activation
{
    BsonDocument _filter;
    BsonDocument _sort;
    BsonDocument _index;

    async Task Because()
    {
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        database.GetCollection<BsonDocument>(MongoConsentRequestStore.CollectionName).Returns(collection);
        collection.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(collection);
        collection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(collection);
        collection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(collection);
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        var render = new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
        collection.FindAsync(Arg.Do<FilterDefinition<BsonDocument>>(_ => _filter = _.Render(render)),
            Arg.Do<FindOptions<BsonDocument, BsonDocument>>(_ => _sort = _.Sort.Render(render)),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        var indexes = Substitute.For<IMongoIndexManager<BsonDocument>>();
        collection.Indexes.Returns(indexes);
        indexes.CreateManyAsync(Arg.Do<IEnumerable<CreateIndexModel<BsonDocument>>>(models => _index = models.ElementAt(1).Keys.Render(render)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));
        await new MongoConsentRequestStore(database).FindPending(binding.Account, binding.Session, binding.Realm, clock.Now, CancellationToken.None);
        await MongoConsentRequestStore.EnsureIndexes(database, CancellationToken.None);
    }

    [Fact] void should_bind_the_account() => _filter["Account"].AsString.ShouldEqual(binding.Account.Value);
    [Fact] void should_bind_the_session() => _filter["Session"].AsString.ShouldEqual(binding.Session);
    [Fact] void should_bind_the_realm() => _filter["Realm"].AsString.ShouldEqual(binding.Realm);
    [Fact] void should_only_read_pending() => _filter["State"].AsString.ShouldEqual(nameof(ConsentRequestState.Pending));
    [Fact] void should_require_the_deadline_after_the_clock() => _filter["Deadline"]["$gt"].ToUniversalTime().ShouldEqual(clock.Now.UtcDateTime);
    [Fact] void should_require_the_deadline_after_database_execution_time() => _filter["$expr"]["$gt"].AsBsonArray.ShouldEqual(new BsonArray { "$Deadline", "$$NOW" });
    [Fact] void should_sort_newest_first() => _sort.ShouldEqual(new BsonDocument("CreatedAt", -1));
    [Fact] void should_index_the_exact_lookup_and_creation_order() => _index.ShouldEqual(new BsonDocument { ["Account"] = 1, ["Session"] = 1, ["Realm"] = 1, ["State"] = 1, ["CreatedAt"] = -1, ["Deadline"] = 1 });
}
