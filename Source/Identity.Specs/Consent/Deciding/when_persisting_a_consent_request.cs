// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow.Operational;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_persisting_a_consent_request : given.an_activation
{
    BsonDocument _inserted;
    BsonDocument _filter;
    BsonDocument _replacement;
    ConsentRequest _roundTrip;
    bool _updated;
    bool _invalidTransition;
    IMongoCollection<BsonDocument> _collection;

    async Task Because()
    {
        var database = Substitute.For<IMongoDatabase>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        database.GetCollection<BsonDocument>(MongoConsentRequestStore.CollectionName).Returns(_collection);
        _collection.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(_collection);
        _collection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(_collection);
        _collection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(_collection);
        _collection.InsertOneAsync(Arg.Do<BsonDocument>(_ => _inserted = _), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var mongo = new MongoConsentRequestStore(database);
        await mongo.Create(request, CancellationToken.None);
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([_inserted]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        _roundTrip = await mongo.Read(request.Id, CancellationToken.None);
        _collection.ReplaceOneAsync(Arg.Do<FilterDefinition<BsonDocument>>(_ => _filter = _.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))),
            Arg.Do<BsonDocument>(_ => _replacement = _),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));
        _updated = await mongo.CompareExchange(_roundTrip, _roundTrip with { State = ConsentRequestState.Decided, Revision = 1 }, CancellationToken.None);
        _invalidTransition = await mongo.CompareExchange(_roundTrip, _roundTrip with { State = ConsentRequestState.Active, Revision = 1, AuthorizationId = "injected" }, CancellationToken.None);
    }

    [Fact] void should_round_trip_the_immutable_binding() => _roundTrip.ShouldEqual(request);
    [Fact] void should_use_the_request_identity_as_the_unique_document_id() => _inserted["_id"].AsString.ShouldEqual(request.Id.Value.ToString("D"));
    [Fact] void should_not_ttl_unresolved_work() => _inserted.Contains("PurgeAt").ShouldBeFalse();
    [Fact] void should_compare_the_revision() => _filter["Revision"].AsInt64.ShouldEqual(0);
    [Fact] void should_compare_the_observed_state() => _filter["State"].AsString.ShouldEqual(nameof(ConsentRequestState.Pending));
    [Fact] void should_compare_the_identity() => _filter["_id"].AsString.ShouldEqual(request.Id.Value.ToString("D"));
    [Fact] void should_increment_the_revision() => _replacement["Revision"].AsInt64.ShouldEqual(1);
    [Fact] void should_acknowledge_a_winning_compare_exchange() => _updated.ShouldBeTrue();
    [Fact] void should_reject_skipping_the_evidence_barrier() => _invalidTransition.ShouldBeFalse();
    [Fact] void should_require_majority_durability() => _collection.Received().WithWriteConcern(WriteConcern.WMajority);
}
