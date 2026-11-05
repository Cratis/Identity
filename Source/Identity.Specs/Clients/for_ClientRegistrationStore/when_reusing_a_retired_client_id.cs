// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_reusing_a_retired_client_id : given.a_client_registration_store
{
    ClientRegistrationOutcome _outcome;
    async Task Establish()
    {
        var previous = await Seed(_confidential);
        await _events.EventLog.Append(previous.Value.ToString("D"), new ClientRetired(new ReasonId("undeclared"), DateTimeOffset.UtcNow));
        await Refresh(previous);
    }

    async Task Because()
    {
        var id = ClientRegistrationId.New();
        _outcome = await _store.Append(id, [new ClientRegistered(ClientId.Create(_confidential.ClientId!), RegistrationKind.PreRegistered, DateTimeOffset.UtcNow)], await _store.Read(id));
    }

    [Fact] void should_release_the_retired_constraint() => _outcome.ShouldEqual(ClientRegistrationOutcome.Success);
}
