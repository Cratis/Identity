// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Authorization.TokenExchange;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard;

public class when_an_insert_loses_the_race : given.a_replay_guard
{
    void Establish()
    {
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        _collection.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<BsonDocument>(new MongoCommandException(connection, "duplicate", new BsonDocument(), new BsonDocument("code", 11000))));
    }

    async Task Because() => _result = await Record();

    [Fact] void should_refuse_the_replay() => _result.ShouldEqual(ReplayRecordResult.AlreadyUsed);
}
