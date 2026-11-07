// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_restriction_write_is_interrupted : given.an_epoch_store
{
    Exception? _tokenCrash;
    Exception? _authorizationCrash;
    bool _tokenRestricted;
    bool _authorizationRestricted;
    bool _oldTokenAccepted;
    bool _oldAuthorizationAccepted;
    bool _newTokenAccepted;
    bool _newAuthorizationAccepted;

    async Task Because()
    {
        _token.Status = Statuses.Valid;
        _authorization.Status = Statuses.Valid;
        await _tokens.UpdateAsync(_token, CancellationToken.None);
        await _authorizations.UpdateAsync(_authorization, CancellationToken.None);
        var tokenSnapshot = _tokenRows.Entry.ToBson();
        var authorizationSnapshot = _authorizationRows.Entry.ToBson();
        _tokenRows.Faults.Arm(FaultPoint.AfterCommit);
        _authorizationRows.Faults.Arm(FaultPoint.AfterCommit);
        _token.Status = Statuses.Revoked;
        _authorization.Status = Statuses.Revoked;
        _tokenCrash = await Catch.Exception(async () => await _tokens.UpdateAsync(_token, CancellationToken.None));
        _authorizationCrash = await Catch.Exception(async () => await _authorizations.UpdateAsync(_authorization, CancellationToken.None));
        _tokenRestricted = _tokenRows.Entry.Status == Statuses.Revoked;
        _authorizationRestricted = _authorizationRows.Entry.Status == Statuses.Revoked;
        _tokenRows.Entry = BsonSerializer.Deserialize<OpenIddictMongoDbToken>(tokenSnapshot);
        _authorizationRows.Entry = BsonSerializer.Deserialize<OpenIddictMongoDbAuthorization>(authorizationSnapshot);
        _gate.ObserveEpoch(8);
        _gate.Open(true);
        _oldTokenAccepted = RecoveryEpochStamp.IsCurrent(await _tokens.GetPropertiesAsync(_tokenRows.Entry, CancellationToken.None), _gate.Epoch);
        _oldAuthorizationAccepted = RecoveryEpochStamp.IsCurrent(await _authorizations.GetPropertiesAsync(_authorizationRows.Entry, CancellationToken.None), _gate.Epoch);
        await _tokens.CreateAsync(new OpenIddictMongoDbToken { Type = TokenTypeHints.AccessToken }, CancellationToken.None);
        await _authorizations.CreateAsync(new OpenIddictMongoDbAuthorization(), CancellationToken.None);
        _newTokenAccepted = RecoveryEpochStamp.IsCurrent(await _tokens.GetPropertiesAsync(_tokenRows.Entry, CancellationToken.None), _gate.Epoch);
        _newAuthorizationAccepted = RecoveryEpochStamp.IsCurrent(await _authorizations.GetPropertiesAsync(_authorizationRows.Entry, CancellationToken.None), _gate.Epoch);
    }

    [Fact] void should_interrupt_the_token_status_write_after_commit() => _tokenCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_interrupt_the_authorization_status_write_after_commit() => _authorizationCrash.ShouldBeOfExactType<InjectedFault>();
    [Fact] void should_actually_commit_the_token_restriction_before_crashing() => _tokenRestricted.ShouldBeTrue();
    [Fact] void should_actually_commit_the_authorization_restriction_before_crashing() => _authorizationRestricted.ShouldBeTrue();
    [Fact] void should_deny_the_restored_token_even_without_its_status_restriction() => _oldTokenAccepted.ShouldBeFalse();
    [Fact] void should_deny_the_restored_authorization_even_without_its_status_restriction() => _oldAuthorizationAccepted.ShouldBeFalse();
    [Fact] void should_accept_a_new_epoch_token() => _newTokenAccepted.ShouldBeTrue();
    [Fact] void should_accept_a_new_epoch_authorization() => _newAuthorizationAccepted.ShouldBeTrue();
}
