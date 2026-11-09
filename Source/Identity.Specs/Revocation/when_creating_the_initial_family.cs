// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.RevocationSpecs;

public class when_creating_the_initial_family : given.a_token_store
{
    GrantState _state;
    async Task Because()
    {
        await _store.CreateAsync(_entry, CancellationToken.None);
        _state = await _fences.Read(_entry.AuthorizationId.ToString(), CancellationToken.None);
    }
    [Fact] void should_preserve_a_preset_id_through_the_stock_insert() => _collection.Received(1).InsertOneAsync(Arg.Is<OpenIddictMongoDbToken>(entry => entry.Id.ToString() == "65ab56ef0123456789012345"), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_admit_the_actual_inserted_id() => _state.CurrentRefreshTokenId.ShouldEqual(_entry.Id.ToString());
    [Fact] void should_record_the_original_absolute_expiry() => _state.Outbox.Single().ExpiresAt.ShouldEqual(new DateTimeOffset(DateTime.UnixEpoch.AddDays(30)));
    [Fact] void should_record_one_family_start() => _state.Outbox.Single().Kind.ShouldEqual(FactKind.FamilyStarted);
}
