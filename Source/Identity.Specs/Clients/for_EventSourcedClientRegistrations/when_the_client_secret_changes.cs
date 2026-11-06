// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_client_secret_changes : given.a_client_registration_store
{
    OpaqueReferenceId _previous;

    async Task Establish()
    {
        var id = await Seed(_confidential);
        _previous = _states.InstanceForEventSourceId(id.Value.ToString("D"))!.CurrentSecretId!;
        _confidential.ClientSecret = "replacement-secret";
    }

    async Task Because() => await Reconcile(_confidential);

    [Fact] void should_append_only_one_rotation() => _store.Appended.Count.ShouldEqual(1);
    [Fact] void should_allocate_a_new_secret_reference() => _store.Appended.OfType<ClientSecretRotated>().Single().ClientSecretId.ShouldNotEqual(_previous);
    [Fact] void should_have_no_overlap() => _store.Appended.OfType<ClientSecretRotated>().Single().PreviousValidUntil.ShouldEqual(_store.Appended.OfType<ClientSecretRotated>().Single().ActivatedAt);
    [Fact] void should_accept_the_replacement() => _digester.Matches(ClientId.Create(_confidential.ClientId!), "replacement-secret", _materialized.Single().State.CurrentSecretHash!).ShouldBeTrue();
    [Fact] void should_refuse_the_previous_secret() => _digester.Matches(ClientId.Create(_confidential.ClientId!), Secret, _materialized.Single().State.CurrentSecretHash!).ShouldBeFalse();
}
