// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_a_client_is_no_longer_declared : given.a_client_registration_store
{
    ClientRegistrationId _id;
    async Task Establish() => _id = await Seed(_confidential);
    async Task Because() => await Reconcile();

    [Fact] void should_retire_the_registration() => _store.Appended.OfType<ClientRetired>().Single().ReasonId.Value.ShouldEqual("undeclared");
    [Fact] void should_remove_the_operational_copy() => _removed.Single().ShouldEqual(_id);
}
