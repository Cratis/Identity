// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Replay.given;
using Cratis.Identity.Revocation;
using MongoDB.Driver;

namespace Cratis.Identity.Replay.for_Revocation;

public class when_rebuilding_read_models_after_a_revocation : RevocationSpecs.given.a_token_store
{
    readonly RevocationReplay _replay = new();
    int _before;
    int _after;

    async Task Because()
    {
        _before = _collection.ReceivedCalls().Count();
        await _replay.Run();
        _after = _collection.ReceivedCalls().Count();
    }

    [Fact] void should_keep_the_same_fence_epoch_sequence_and_outbox() => JsonSerializer.Serialize(_replay.After).ShouldEqual(JsonSerializer.Serialize(_replay.Before));
    [Fact] void should_refuse_refresh_after_rebuild() => _replay.Redemption.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_not_mint_or_insert_any_token() => _after.ShouldEqual(_before);
    [Fact] void should_keep_one_chronicle_fact_per_intent() => _replay.Appended.ShouldEqual(_replay.Before.Outbox.Length);
}
