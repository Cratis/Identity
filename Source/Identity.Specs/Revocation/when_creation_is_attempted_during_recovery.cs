// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.RevocationSpecs;

public class when_creation_is_attempted_during_recovery : given.an_epoch_store
{
    Exception? _tokenFailure;
    Exception? _authorizationFailure;

    async Task Because()
    {
        _tokenRows.Collection.ClearReceivedCalls();
        _authorizationRows.Collection.ClearReceivedCalls();
        _gate.Close();
        _tokenFailure = await Catch.Exception(async () => await _tokens.CreateAsync(new OpenIddictMongoDbToken(), CancellationToken.None));
        _authorizationFailure = await Catch.Exception(async () => await _authorizations.CreateAsync(new OpenIddictMongoDbAuthorization(), CancellationToken.None));
    }

    [Fact] void should_deny_token_creation() => _tokenFailure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_deny_authorization_creation() => _authorizationFailure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_insert_a_token() => _tokenRows.Collection.DidNotReceive().InsertOneAsync(Arg.Any<OpenIddictMongoDbToken>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_insert_an_authorization() => _authorizationRows.Collection.DidNotReceive().InsertOneAsync(Arg.Any<OpenIddictMongoDbAuthorization>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
