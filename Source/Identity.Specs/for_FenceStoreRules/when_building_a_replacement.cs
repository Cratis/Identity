// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_building_a_replacement : Specification
{
    GrantState _clear;
    GrantState _next;
    GrantFence _fence;
    GrantState _fenced;
    GrantState _delivered;
    BsonDocument _firstFilter;
    BsonDocument _updateFilter;
    BsonDocument _fencingFilter;
    BsonDocument _fencedFilter;
    BsonDocument _document;
    BsonDocument _fencedDocument;
    BsonDocument _replayDocument;

    void Establish()
    {
        _clear = FenceStoreFixtures.Clear();
        _next = _clear with { Version = 2, CurrentRefreshTokenId = "successor" };
        _fence = FenceStoreFixtures.Fence();
        _fenced = FenceStoreFixtures.Fenced(_fence);
        _delivered = _clear with { Outbox = [.. _clear.Outbox.Select(entry => entry with { State = OutboxState.Delivered })] };
    }

    void Because()
    {
        _firstFilter = FenceStoreRules.ReplacementFilter(null, _clear);
        _updateFilter = FenceStoreRules.ReplacementFilter(_clear, _next);
        _fencingFilter = FenceStoreRules.ReplacementFilter(_clear, _fenced);
        _fencedFilter = FenceStoreRules.ReplacementFilter(_fenced, _fenced);
        _document = FenceStoreRules.ReplacementDocument(_next, null);
        _fencedDocument = FenceStoreRules.ReplacementDocument(_fenced, null);
        _replayDocument = FenceStoreRules.ReplacementDocument(_next, 7);
    }

    [Fact] void should_only_create_when_no_payload_exists() => _firstFilter.ShouldEqual(new BsonDocument { { "_id", "grant" }, { "payload", new BsonDocument("$exists", false) } });
    [Fact] void should_compare_and_set_on_the_expected_revision() => _updateFilter.ShouldEqual(new BsonDocument { { "_id", "grant" }, { "version", 1L }, { "fenced", false }, { "currentRefreshTokenId", "token" } });
    [Fact] void should_compare_a_missing_token_as_null() => _fencedFilter["currentRefreshTokenId"].IsBsonNull.ShouldBeTrue();
    [Fact] void should_refuse_a_fence_whose_reservation_was_cancelled() => _fencingFilter["cancelledReservations"].ShouldEqual(new BsonDocument("$ne", _fence.Id.ToString("D")));
    [Fact] void should_not_constrain_cancellations_for_a_non_fencing_write() => _updateFilter.Contains("cancelledReservations").ShouldBeFalse();
    [Fact] void should_set_the_indexed_columns_then_the_payload() => _document.Names.ToArray().ShouldEqual(["version", "fenced", "fenceId", "currentRefreshTokenId", "undelivered", "payload"]);
    [Fact] void should_set_the_columns() => _document["currentRefreshTokenId"].AsString.ShouldEqual("successor");
    [Fact] void should_serialize_the_state_as_the_payload() => _document["payload"].AsString.ShouldEqual(JsonSerializer.Serialize(_next));
    [Fact] void should_flag_an_undelivered_family_start() => _document["undelivered"].AsBoolean.ShouldBeTrue();
    [Fact] void should_flag_a_fully_delivered_outbox_as_delivered() => FenceStoreRules.ReplacementDocument(_delivered, null)["undelivered"].AsBoolean.ShouldBeFalse();
    [Fact] void should_record_the_fence_identity() => _fencedDocument["fenceId"].AsString.ShouldEqual(_fence.Id.ToString("D"));
    [Fact] void should_flag_pending_restrictions_as_undelivered() => _fencedDocument["undelivered"].AsBoolean.ShouldBeTrue();
    [Fact] void should_null_the_token_of_a_fenced_grant() => _fencedDocument["currentRefreshTokenId"].IsBsonNull.ShouldBeTrue();
    [Fact] void should_not_write_a_replay_epoch_by_default() => _document.Contains("replayEpoch").ShouldBeFalse();
    [Fact] void should_append_the_replay_epoch_last() => _replayDocument.Names.Last().ShouldEqual("replayEpoch");
}
