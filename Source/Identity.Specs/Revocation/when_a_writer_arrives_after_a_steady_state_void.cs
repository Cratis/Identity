// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_writer_arrives_after_a_steady_state_void : given.a_steady_state_reservation
{
    MongoFenceStore _writer;
    BsonDocument _cancelFilter;
    BsonDocument _cancelUpdate;
    BsonDocument _commitFilter;
    Exception? _refusal;

    void Establish()
    {
        var database = Substitute.For<IMongoDatabase>();
        var grants = Substitute.For<IMongoCollection<BsonDocument>>();
        database.GetCollection<BsonDocument>("grant-fences").Returns(grants);
        grants.WithReadPreference(Arg.Any<ReadPreference>()).Returns(grants);
        grants.WithReadConcern(Arg.Any<ReadConcern>()).Returns(grants);
        grants.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(grants);
        _writer = new(database, _time, _log);
        _authority.Cancel(_reservation, Arg.Any<CancellationToken>()).Returns(_ => _writer.Cancel(_reservation, CancellationToken.None));
        grants.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var args = new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
            var filter = call.Arg<FilterDefinition<BsonDocument>>().Render(args);
            var update = call.Arg<UpdateDefinition<BsonDocument>>().Render(args).AsBsonDocument;
            if (update.Contains("$addToSet"))
            {
                _cancelFilter = filter;
                _cancelUpdate = update;
                return new UpdateResult.Acknowledged(0, 0, _reservation.GrantId);
            }

            _commitFilter = filter;
            return new UpdateResult.Acknowledged(0, 0, null);
        });
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([new BsonDocument("cancelledReservations", new BsonArray { _reservation.Id.ToString("D") })]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        grants.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }

    async Task Because()
    {
        await Run();
        var fence = new GrantFence(_reservation.Id, _reservation.Sequence, _reservation.GrantId, "delayed-writer", RevocationReason.Revoked, _time.GetUtcNow());
        _refusal = await Catch.Exception(() => _writer.CommitReserved(null, fence, CancellationToken.None));
    }

    [Fact] void should_atomically_require_that_the_commit_has_not_won() => _cancelFilter["fenceId"]["$ne"].AsString.ShouldEqual(_reservation.Id.ToString("D"));
    [Fact] void should_persist_the_cancellation_marker() => _cancelUpdate["$addToSet"]["cancelledReservations"].AsString.ShouldEqual(_reservation.Id.ToString("D"));
    [Fact] void should_require_the_marker_to_be_absent_when_committing() => _commitFilter["cancelledReservations"]["$ne"].AsString.ShouldEqual(_reservation.Id.ToString("D"));
    [Fact] void should_refuse_the_delayed_writer() => _refusal.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_keep_the_reservation_voided() => _state.ShouldEqual(ReservationState.Voided);
    [Fact] void should_leave_the_contiguous_prefix_advanced() => _checkpoint.AppliedThrough.ShouldEqual(3);
    [Fact] void should_not_allocate_a_replacement_reservation() => _log.DidNotReceive().Reserve(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
