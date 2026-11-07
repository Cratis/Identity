// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_insert_is_interrupted_after_commit : given.an_epoch_store
{
    Exception? _tokenCrash;
    Exception? _authorizationCrash;

    async Task Because()
    {
        _tokenRows.Faults.Arm(FaultPoint.AfterCommit);
        _authorizationRows.Faults.Arm(FaultPoint.AfterCommit);
        _tokenCrash = await Catch.Exception(async () => await _tokens.CreateAsync(new OpenIddictMongoDbToken { Type = TokenTypeHints.AccessToken }, CancellationToken.None));
        _authorizationCrash = await Catch.Exception(async () => await _authorizations.CreateAsync(new OpenIddictMongoDbAuthorization(), CancellationToken.None));
    }

    [Fact] void should_crash_after_the_token_insert() => _tokenCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_crash_after_the_authorization_insert() => _authorizationCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_have_already_persisted_the_token_stamp() => _tokenRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
    [Fact] void should_have_already_persisted_the_authorization_stamp() => _authorizationRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
}
