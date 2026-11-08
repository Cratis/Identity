// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Identity.Authorization;

namespace Cratis.Identity.RevocationSpecs;

public class when_epoch_properties_are_replaced : given.an_epoch_store
{
    string _tokenCustom;
    string _authorizationCustom;

    async Task Because()
    {
        var properties = ImmutableDictionary<string, JsonElement>.Empty.Add("custom", JsonSerializer.SerializeToElement("value"));
        await _tokens.SetPropertiesAsync(_token, properties, CancellationToken.None);
        await _authorizations.SetPropertiesAsync(_authorization, properties, CancellationToken.None);
        await _tokens.UpdateAsync(_token, CancellationToken.None);
        await _authorizations.UpdateAsync(_authorization, CancellationToken.None);
        _tokenCustom = _tokenRows.Entry.Properties["custom"].AsString;
        _authorizationCustom = _authorizationRows.Entry.Properties["custom"].AsString;
        _gate.ObserveEpoch(8);
        _gate.Open(true);
        await _tokens.SetPropertiesAsync(_token, [], CancellationToken.None);
        await _authorizations.SetPropertiesAsync(_authorization, [], CancellationToken.None);
        await _tokens.UpdateAsync(_token, CancellationToken.None);
        await _authorizations.UpdateAsync(_authorization, CancellationToken.None);
    }

    [Fact] void should_replace_custom_token_properties() => _tokenCustom.ShouldEqual("value");
    [Fact] void should_replace_custom_authorization_properties() => _authorizationCustom.ShouldEqual("value");
    [Fact] void should_preserve_the_inserted_token_epoch_through_both_updates() => _tokenRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
    [Fact] void should_preserve_the_inserted_authorization_epoch_through_both_updates() => _authorizationRows.Entry.Properties[RecoveryEpochStamp.PropertyName].AsInt64.ShouldEqual(7);
    [Fact] void should_clear_other_token_properties() => _tokenRows.Entry.Properties.ElementCount.ShouldEqual(1);
    [Fact] void should_clear_other_authorization_properties() => _authorizationRows.Entry.Properties.ElementCount.ShouldEqual(1);
}
