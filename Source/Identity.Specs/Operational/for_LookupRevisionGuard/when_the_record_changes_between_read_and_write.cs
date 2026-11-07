// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Identity.Operational.for_LookupRevisionGuard;

public class when_the_record_changes_between_read_and_write : given.a_revision_guard
{
    void Establish()
    {
        Stored([new(LookupRevision.SingletonId, _key.Check, _key.Revision)], []);
        _collection.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<LookupRevision>>(), Arg.Any<UpdateDefinition<LookupRevision>>(), Arg.Any<FindOneAndUpdateOptions<LookupRevision, LookupRevision>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<LookupRevision>(null!));
    }

    Task Because() => Start();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<LookupKeyRevisionMismatch>();
    [Fact] void should_not_retry_the_write() => Writes.ShouldEqual(1);
}
