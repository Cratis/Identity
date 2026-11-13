// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_a_stored_document_has_no_payload : Specification
{
    bool _withCancellation;
    bool _withEmptyCancellations;
    bool _withoutCancellations;

    void Because()
    {
        _withCancellation = FenceStoreRules.IsCancellationOnly(new BsonDocument { { "_id", "grant" }, { "cancelledReservations", new BsonArray { "x" } } });
        _withEmptyCancellations = FenceStoreRules.IsCancellationOnly(new BsonDocument { { "_id", "grant" }, { "cancelledReservations", new BsonArray() } });
        _withoutCancellations = FenceStoreRules.IsCancellationOnly(new BsonDocument("_id", "grant"));
    }

    [Fact] void should_treat_a_cancellation_that_won_early_as_absent_authority() => _withCancellation.ShouldBeTrue();
    [Fact] void should_not_treat_an_empty_marker_list_as_a_cancellation() => _withEmptyCancellations.ShouldBeFalse();
    [Fact] void should_not_treat_a_missing_marker_list_as_a_cancellation() => _withoutCancellations.ShouldBeFalse();
}
