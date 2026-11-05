// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_another_replica_registered_first : given.a_client_registration_store
{
    void Establish()
    {
        _store.Outcomes.Enqueue(ClientRegistrationOutcome.AlreadyRegistered);
        _store.OnRejected = async () => await Seed(_confidential);
    }

    async Task Because() => await Reconcile(_confidential);

    [Fact] void should_not_append_a_duplicate_registration() => _store.Appended.OfType<ClientRegistered>().ShouldBeEmpty();
    [Fact] void should_materialize_the_winning_registration() => _materialized.Count.ShouldEqual(1);
    [Fact] void should_reread_before_the_diff() => _store.Attempts.ShouldEqual(1);
}
