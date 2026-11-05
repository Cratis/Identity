// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_redirect_set_changes : given.a_client_registration_store
{
    ClientRegistrationId _id;
    async Task Establish()
    {
        _id = await Seed(_cli);
        _cli.RedirectUris.Add(new("https://changed.example/callback"));
    }

    async Task Because() => await Reconcile(_cli);

    [Fact] void should_append_only_a_redirect_change() => _store.Appended.Single().ShouldBeOfExactType<ClientRedirectUrisChanged>();
    [Fact] void should_preserve_the_registration_identity() => _materialized.Single().State.ClientRegistrationId.ShouldEqual(_id);
    [Fact] void should_materialize_the_new_redirect_set() => _materialized.Single().State.RedirectUris.Count().ShouldEqual(3);
}
