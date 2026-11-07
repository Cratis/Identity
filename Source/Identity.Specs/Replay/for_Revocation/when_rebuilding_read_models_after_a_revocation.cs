// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Replay.given;
using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Replay.for_Revocation;

public class when_rebuilding_read_models_after_a_revocation : RevocationSpecs.given.a_token_store
{
    RevocationReplay _replay;
    int _insertions;
    int _controlInsertions;
    Exception? _creationFailure;

    async Task Because()
    {
        _collection.When(collection => collection.InsertOneAsync(Arg.Any<OpenIddictMongoDbToken>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()))
            .Do(_ => _insertions++);
        await _store.CreateAsync(_entry, CancellationToken.None);
        _controlInsertions = _insertions;
        _replay = new(_fences, _entry.AuthorizationId.ToString(), _entry.Id.ToString());
        await _replay.Run();
        _creationFailure = await Catch.Exception(async () => await _store.CreateAsync(_entry, CancellationToken.None));
    }

    [Fact] void should_keep_the_same_fence_epoch_sequence_and_outbox() => JsonSerializer.Serialize(_replay.After).ShouldEqual(JsonSerializer.Serialize(_replay.Before));
    [Fact] void should_refuse_refresh_after_rebuild() => _replay.Redemption.ShouldEqual(RedeemOutcome.Fenced);
    [Fact] void should_record_a_real_token_insert_before_revocation() => _controlInsertions.ShouldEqual(1);
    [Fact] void should_not_insert_any_token_during_rebuild_or_revoked_creation() => _insertions.ShouldEqual(1);
    [Fact] void should_refuse_token_creation_after_rebuild() => _creationFailure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_keep_one_chronicle_fact_per_intent() => _replay.Appended.ShouldEqual(_replay.Before.Outbox.Length);
}
