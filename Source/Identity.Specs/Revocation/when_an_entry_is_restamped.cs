// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_an_entry_is_restamped : given.an_epoch_store
{
    Exception? _tokenChange;
    Exception? _authorizationChange;
    Exception? _tokenRemoval;
    Exception? _authorizationRemoval;
    Exception? _tokenUpdate;
    Exception? _authorizationUpdate;
    Exception? _tokenClear;
    Exception? _authorizationClear;

    async Task Because()
    {
        _gate.ObserveEpoch(8);
        _gate.Open(true);
        var changed = ImmutableDictionary<string, JsonElement>.Empty.Add(RecoveryEpochStamp.PropertyName, JsonSerializer.SerializeToElement(8L));
        var removed = changed.SetItem(RecoveryEpochStamp.PropertyName, JsonSerializer.SerializeToElement<object?>(null));
        _tokenChange = await Catch.Exception(async () => await _tokens.SetPropertiesAsync(_token, changed, CancellationToken.None));
        _authorizationChange = await Catch.Exception(async () => await _authorizations.SetPropertiesAsync(_authorization, changed, CancellationToken.None));
        _tokenRemoval = await Catch.Exception(async () => await _tokens.SetPropertiesAsync(_token, removed, CancellationToken.None));
        _authorizationRemoval = await Catch.Exception(async () => await _authorizations.SetPropertiesAsync(_authorization, removed, CancellationToken.None));
        _token.Properties[RecoveryEpochStamp.PropertyName] = 8L;
        _authorization.Properties[RecoveryEpochStamp.PropertyName] = 8L;
        _tokenUpdate = await Catch.Exception(async () => await _tokens.UpdateAsync(_token, CancellationToken.None));
        _authorizationUpdate = await Catch.Exception(async () => await _authorizations.UpdateAsync(_authorization, CancellationToken.None));
        _token.Properties = null;
        _authorization.Properties = null;
        _tokenClear = await Catch.Exception(async () => await _tokens.UpdateAsync(_token, CancellationToken.None));
        _authorizationClear = await Catch.Exception(async () => await _authorizations.UpdateAsync(_authorization, CancellationToken.None));
    }

    [Fact] void should_reject_a_token_property_restamp() => _tokenChange.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_an_authorization_property_restamp() => _authorizationChange.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_explicit_token_stamp_removal() => _tokenRemoval.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_explicit_authorization_stamp_removal() => _authorizationRemoval.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_direct_token_restamping_at_update() => _tokenUpdate.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_direct_authorization_restamping_at_update() => _authorizationUpdate.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_direct_token_stamp_removal_at_update() => _tokenClear.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_reject_direct_authorization_stamp_removal_at_update() => _authorizationClear.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_keep_the_original_persisted_token_stamp() => _tokenRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
    [Fact] void should_keep_the_original_persisted_authorization_stamp() => _authorizationRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
}
