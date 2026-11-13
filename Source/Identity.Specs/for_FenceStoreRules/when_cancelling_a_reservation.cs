// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_cancelling_a_reservation : Specification
{
    FenceReservation _reservation;
    BsonDocument _filter;
    BsonDocument _update;
    bool _matched;
    bool _upserted;
    bool _modifiedOnly;
    bool _unmatched;
    bool _multiple;
    bool _unacknowledged;

    void Establish() => _reservation = new(Guid.NewGuid(), 4, "grant", default, DateTimeOffset.UnixEpoch);

    void Because()
    {
        _filter = FenceStoreRules.CancellationFilter(_reservation);
        _update = FenceStoreRules.CancellationUpdate(_reservation);
        _matched = FenceStoreRules.IsCommitted(new UpdateResult.Acknowledged(1, 1, null));
        _upserted = FenceStoreRules.IsCommitted(new UpdateResult.Acknowledged(0, 0, new BsonString("grant")));
        _modifiedOnly = FenceStoreRules.IsCommitted(new UpdateResult.Acknowledged(0, 1, null));
        _unmatched = FenceStoreRules.IsCommitted(new UpdateResult.Acknowledged(0, 0, null));
        _multiple = FenceStoreRules.IsCommitted(new UpdateResult.Acknowledged(2, 2, null));
        _unacknowledged = FenceStoreRules.IsCommitted(UpdateResult.Unacknowledged.Instance);
    }

    [Fact] void should_not_match_a_grant_already_fenced_by_that_reservation() => _filter.ShouldEqual(new BsonDocument { { "_id", "grant" }, { "fenceId", new BsonDocument("$ne", _reservation.Id.ToString("D")) } });
    [Fact] void should_add_the_marker_to_the_set() => _update.ShouldEqual(new BsonDocument("$addToSet", new BsonDocument("cancelledReservations", _reservation.Id.ToString("D"))));
    [Fact] void should_commit_a_single_match() => _matched.ShouldBeTrue();
    [Fact] void should_commit_an_upsert() => _upserted.ShouldBeTrue();
    [Fact] void should_not_commit_a_modification_without_a_match() => _modifiedOnly.ShouldBeFalse();
    [Fact] void should_not_commit_without_a_match() => _unmatched.ShouldBeFalse();
    [Fact] void should_not_commit_more_than_one_match() => _multiple.ShouldBeFalse();
    [Fact] void should_not_commit_an_unacknowledged_write() => _unacknowledged.ShouldBeFalse();
}
