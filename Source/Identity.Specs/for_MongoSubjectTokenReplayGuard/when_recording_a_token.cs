// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard;

public class when_recording_a_token : given.a_replay_guard
{
    async Task Because() => _result = await Record();

    [Fact] void should_record_the_first_use() => _result.ShouldEqual(ReplayRecordResult.Recorded);
    [Fact] void should_use_the_verified_key_verbatim() => _filter["_id"].AsString.ShouldEqual(Key);
    [Fact] void should_allow_replacing_an_expired_entry_despite_ttl_lag() => _filter["Until"]["$lte"].ToUniversalTime().ShouldEqual(_time.Now.UtcDateTime);
    [Fact] void should_store_only_the_expiration_update() => _update.ShouldEqual(new BsonDocument("$set", new BsonDocument("Until", new BsonDateTime(_until.UtcDateTime))));
    [Fact] void should_upsert_atomically() => _options.IsUpsert.ShouldBeTrue();
    [Fact] void should_require_majority_writes() => _collection.Received(1).WithWriteConcern(WriteConcern.WMajority);
    [Fact] void should_require_majority_reads() => _collection.Received(1).WithReadConcern(ReadConcern.Majority);
    [Fact] void should_use_the_primary() => _collection.Received(1).WithReadPreference(ReadPreference.Primary);
}
