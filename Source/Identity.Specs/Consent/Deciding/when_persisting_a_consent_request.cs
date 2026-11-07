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
    BsonDocument _activeFilter;
    BsonDocument _replacement;
    ConsentRequest _roundTrip;
    ConsentRequest _legacy;
    bool _epochChange;
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
        request = request with { RecoveryEpoch = 7 };
        await mongo.Create(request, CancellationToken.None);
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([_inserted]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        _roundTrip = await mongo.Read(request.Id, CancellationToken.None);
        _inserted.Remove("RecoveryEpoch");
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _legacy = await mongo.Read(request.Id, CancellationToken.None);
        _inserted["RecoveryEpoch"] = 7L;
        _collection.ReplaceOneAsync(Arg.Do<FilterDefinition<BsonDocument>>(CaptureFilter),
            Arg.Do<BsonDocument>(_ => _replacement ??= _),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));
        _updated = await mongo.CompareExchange(_roundTrip, _roundTrip with { State = ConsentRequestState.Decided, Revision = 1 }, clock.Now, CancellationToken.None);
        _epochChange = await mongo.CompareExchange(_roundTrip, _roundTrip with { State = ConsentRequestState.Decided, Revision = 1, RecoveryEpoch = 8 }, clock.Now, CancellationToken.None);
        _invalidTransition = await mongo.CompareExchange(_roundTrip, _roundTrip with { State = ConsentRequestState.Active, Revision = 1, AuthorizationId = "injected" }, clock.Now, CancellationToken.None);
        var evidenced = _roundTrip with { State = ConsentRequestState.EvidenceRecorded, Revision = 2 };
        var active = evidenced with { State = ConsentRequestState.Active, Revision = 3, AuthorizationId = "prepared" };
        await mongo.CompareExchange(evidenced, active, clock.Now, CancellationToken.None);
    }

    void CaptureFilter(FilterDefinition<BsonDocument> filter)
    {
        var document = filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
        if (document.Contains("$expr")) _activeFilter = document;
        else _filter = document;
    }

    [Fact] void should_atomically_require_the_deadline_after_the_supplied_time() => _activeFilter["Deadline"]["$gt"].ToUniversalTime().ShouldEqual(clock.Now.UtcDateTime);
    [Fact] void should_also_compare_the_deadline_to_mongodb_execution_time() => _activeFilter["$expr"]["$gt"].AsBsonArray.ShouldEqual(new BsonArray { "$Deadline", "$$NOW" });
    [Fact] void should_store_only_the_opaque_authorization_request_identity() => _inserted["AuthorizationRequest"].AsString.ShouldEqual(binding.AuthorizationRequest.Value.ToString("D"));
    [Fact] void should_round_trip_the_immutable_binding() => _roundTrip.ShouldEqual(request);
    [Fact] void should_use_the_request_identity_as_the_unique_document_id() => _inserted["_id"].AsString.ShouldEqual(request.Id.Value.ToString("D"));
    [Fact] void should_not_ttl_unresolved_work() => _inserted.Contains("PurgeAt").ShouldBeFalse();
    [Fact] void should_persist_the_creation_epoch_in_the_insert() => _inserted["RecoveryEpoch"].AsInt64.ShouldEqual(7);
    [Fact] void should_compare_the_observed_epoch() => _filter["RecoveryEpoch"].AsInt64.ShouldEqual(7);
    [Fact] void should_read_a_legacy_row_as_epoch_zero() => _legacy.RecoveryEpoch.ShouldEqual(0);
    [Fact] void should_reject_changing_the_creation_epoch() => _epochChange.ShouldBeFalse();
    [Fact] void should_compare_the_revision() => _filter["Revision"].AsInt64.ShouldEqual(0);
    [Fact] void should_compare_the_observed_state() => _filter["State"].AsString.ShouldEqual(nameof(ConsentRequestState.Pending));
    [Fact] void should_compare_the_identity() => _filter["_id"].AsString.ShouldEqual(request.Id.Value.ToString("D"));
    [Fact] void should_increment_the_revision() => _replacement["Revision"].AsInt64.ShouldEqual(1);
    [Fact] void should_acknowledge_a_winning_compare_exchange() => _updated.ShouldBeTrue();
    [Fact] void should_reject_skipping_the_evidence_barrier() => _invalidTransition.ShouldBeFalse();
    [Fact] void should_require_majority_durability() => _collection.Received().WithWriteConcern(WriteConcern.WMajority);
}
