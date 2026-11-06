// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_registering_a_duplicate_client : given.a_client_registration_store
{
    ClientRegistrationOutcome _outcome;
    async Task Establish() => await Seed(_confidential);
    async Task Because()
    {
        var id = ClientRegistrationId.New();
        var read = await _store.Read(id);
        _outcome = await _store.Append(id, [new ClientRegistered(ClientId.Create(_confidential.ClientId!), RegistrationKind.PreRegistered, DateTimeOffset.UtcNow)], read);
    }

    [Fact] void should_enforce_the_cross_stream_constraint() => _outcome.ShouldEqual(ClientRegistrationOutcome.AlreadyRegistered);
    [Fact] void should_not_append_the_duplicate() => _store.Appended.ShouldBeEmpty();
}
