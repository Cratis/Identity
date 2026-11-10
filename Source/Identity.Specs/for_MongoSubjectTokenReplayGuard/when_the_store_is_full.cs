// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Operational;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard;

public class when_the_store_is_full : given.a_replay_guard
{
    void Establish() => _collection.EstimatedDocumentCountAsync(Arg.Any<EstimatedDocumentCountOptions>(), Arg.Any<CancellationToken>()).Returns(MongoSubjectTokenReplayGuard.MaximumEntries);

    async Task Because() => _result = await Record();

    [Fact] void should_fail_closed() => _result.ShouldEqual(ReplayRecordResult.Full);
    [Fact] void should_not_attempt_a_write() => _collection.DidNotReceive().FindOneAndUpdateAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>());
}
