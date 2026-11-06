// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_ClientRegistrationStore;

public class when_the_guard_is_stale : given.a_client_registration_store
{
    ClientRegistrationId _id;
    ClientRegistrationOutcome _outcome;
    async Task Establish() => _id = await Seed(_confidential);
    async Task Because()
    {
        var read = await _store.Read(_id);
        await _events.EventLog.Append(_id.Value.ToString("D"), new ClientRedirectUrisChanged([RedirectUri.Create("https://other.example/callback")]));
        _outcome = await _store.Append(_id, [new ClientSecretRotated(OpaqueReferenceId.New(), new("v1.fake"), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)], read);
    }

    [Fact] void should_reject_a_changed_stream() => _outcome.ShouldEqual(ClientRegistrationOutcome.Conflict);
    [Fact] void should_not_append_the_stale_decision() => _store.Appended.ShouldBeEmpty();
}
