// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_successor_insert_is_ordered_after_revoke : given.a_token_store
{
    Exception? _error;
    async Task Establish()
    {
        _issuance.GrantId = _entry.AuthorizationId.ToString();
        _issuance.SuccessorId = _entry.Id.ToString();
        await _fences.RecordFamilyStart(_issuance.GrantId, _issuance.SuccessorId, FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        await _fences.Commit(_issuance.GrantId, FenceDoubles.Subject, RevocationReason.Revoked, CancellationToken.None);
    }
    async Task Because() => _error = await Catch.Exception(async () => await _store.CreateAsync(_entry, CancellationToken.None));
    [Fact] void should_not_issue_the_previously_installed_successor() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_insert_an_entry() => _collection.DidNotReceive().InsertOneAsync(Arg.Any<OpenIddictMongoDbToken>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
