// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_successor_insert_is_ordered_after_revoke : given.a_token_store
{
    GrantState _state;
    RedeemOutcome _nextUse;

    async Task Because()
    {
        var grantId = _entry.AuthorizationId.ToString();
        var successor = _entry.Id.ToString();
        var state = await _fences.RecordFamilyStart(grantId, "old-entry", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(20), CancellationToken.None);
        await _fences.RedeemAndInstall(grantId, "old-entry", successor, state.Version, CancellationToken.None);
        _issuance.Admit(state, successor);
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var insert = InsertSuccessor();
        await held.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _fences.Commit(grantId, FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        release.SetResult();
        await insert;
        _state = await _fences.Read(grantId, CancellationToken.None);
        _nextUse = await _fences.RedeemAndInstall(grantId, successor, "another-entry", _state.Version, CancellationToken.None);

        async Task InsertSuccessor()
        {
            held.SetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            await _store.CreateAsync(_entry, CancellationToken.None);
        }
    }

    [Fact] void should_insert_the_already_admitted_successor() => _collection.Received(1).InsertOneAsync(Arg.Is<OpenIddictMongoDbToken>(entry => entry.Id.ToString() == _issuance.SuccessorId), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_retain_the_original_absolute_expiry() => _entry.ExpirationDate.ShouldEqual(DateTime.UnixEpoch.AddDays(20));
    [Fact] void should_keep_the_family_fenced() => _state.Fence.Reason.ShouldEqual(RevocationReason.RefreshTokenReused);
    [Fact] void should_deny_the_successor_on_next_use() => _nextUse.ShouldEqual(RedeemOutcome.Fenced);
}
