// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;
using Cratis.Identity.Operational;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard.given;

public class a_replay_guard : Specification
{
    protected IMongoDatabase _database;
    protected IMongoCollection<BsonDocument> _collection;
    protected FixedTimeProvider _time;
    private protected MongoSubjectTokenReplayGuard _guard;
    private protected ReplayRecordResult _result;
    protected BsonDocument _filter;
    protected BsonDocument _update;
    protected FindOneAndUpdateOptions<BsonDocument, BsonDocument> _options;
    protected DateTimeOffset _until;
    protected const string Key = "https://issuer.example/\nsha256:ABCDEF";

    void Establish()
    {
        _time = new(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        _until = _time.Now.AddMinutes(5);
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(_collection);
        _collection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(_collection);
        _collection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(_collection);
        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<BsonDocument>(MongoSubjectTokenReplayGuard.CollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_collection);
        _collection.FindOneAndUpdateAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(_ => _filter = _.Render(RenderArguments)),
            Arg.Do<UpdateDefinition<BsonDocument>>(_ => _update = _.Render(RenderArguments).AsBsonDocument),
            Arg.Do<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(_ => _options = _),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<BsonDocument>(null!));
        _guard = new(_database, _time);
    }

    protected static RenderArgs<BsonDocument> RenderArguments => new(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);

    private protected Task<ReplayRecordResult> Record(CancellationToken cancellationToken = default) => _guard.Record(Key, _until, cancellationToken);
}
