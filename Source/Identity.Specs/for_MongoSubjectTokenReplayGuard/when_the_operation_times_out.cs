// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using MongoDB.Driver;

namespace Cratis.Identity.for_MongoSubjectTokenReplayGuard;

public class when_the_operation_times_out : given.a_replay_guard
{
    void Establish() => _collection.EstimatedDocumentCountAsync(Arg.Any<EstimatedDocumentCountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(new OperationCanceledException()));

    async Task Because() => _result = await Record();

    [Fact] void should_fail_closed() => _result.ShouldEqual(ReplayRecordResult.Unavailable);
}
