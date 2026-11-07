// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.RevocationSpecs;

public class when_creating_an_installed_successor : given.a_token_store
{
    GrantState _state;
    async Task Establish()
    {
        var grantId = _entry.AuthorizationId.ToString();
        var state = await _fences.RecordFamilyStart(grantId, "old-entry", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(20), CancellationToken.None);
        const string successor = "65ab56ef0123456789012350";
        await _fences.RedeemAndInstall(grantId, "old-entry", successor, state.Version, CancellationToken.None);
        _issuance.Admit(state, successor);
    }
    async Task Because()
    {
        await _store.CreateAsync(_entry, CancellationToken.None);
        _state = await _fences.Read(_entry.AuthorizationId.ToString(), CancellationToken.None);
    }
    [Fact] void should_insert_the_preallocated_id_not_an_unrelated_new_one() => _collection.Received(1).InsertOneAsync(Arg.Is<OpenIddictMongoDbToken>(entry => entry.Id.ToString() == _issuance.SuccessorId), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_never_slide_the_absolute_expiry() => _entry.ExpirationDate.ShouldEqual(DateTime.UnixEpoch.AddDays(20));
    [Fact] void should_not_emit_a_rotation_fact_or_second_family_start() => _state.Outbox.Length.ShouldEqual(1);
}
