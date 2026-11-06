// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_folding_client_versions : given.a_client_registration_store
{
    ClientRegistrationId _id;
    EventSequenceNumber _expected;
    ClientRegistrationState _state;

    async Task Establish() => _id = await Seed(_confidential);

    async Task Because()
    {
        await _events.EventLog.Append(_id.Value.ToString("D"), new ClientRedirectUrisChanged([RedirectUri.Create("https://updated.example/callback")]));
        await Refresh(_id);
        var types = new[] { typeof(ClientRegistered), typeof(ClientSecretRotated), typeof(ClientRedirectUrisChanged) }.Select(_events.EventTypes.GetEventTypeFor).ToArray();
        var events = await _events.EventLog.GetForEventSourceIdAndEventTypes(_id.Value.ToString("D"), types);
        _expected = events[^1].Context.SequenceNumber;
        _state = (await _store.Read(_id)).State;
    }

    [Fact] void should_use_the_last_fact_sequence_as_the_authoritative_version() => _state.Version.ShouldEqual(_expected);
}
