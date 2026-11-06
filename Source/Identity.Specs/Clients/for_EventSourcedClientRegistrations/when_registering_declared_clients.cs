// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_registering_declared_clients : given.a_client_registration_store
{
    async Task Because() => await Reconcile(_cli, _confidential);

    [Fact] void should_register_both_clients() => _store.Appended.OfType<ClientRegistered>().Count().ShouldEqual(2);
    [Fact] void should_record_the_cli_redirects() => _store.Appended.OfType<ClientRedirectUrisChanged>().Single().RedirectUris.Count().ShouldEqual(2);
    [Fact] void should_record_only_the_confidential_secret_metadata() => _store.Appended.OfType<ClientSecretRotated>().Count().ShouldEqual(1);
    [Fact] void should_materialize_both_from_committed_facts() => _materialized.Count.ShouldEqual(2);
    [Fact] void should_keep_descriptor_permissions() => _materialized[0].Policy.Permissions.ShouldContain(OpenIddict.Abstractions.OpenIddictConstants.Permissions.Endpoints.Token);
    [Fact] void should_materialize_the_fact_redirects() => _materialized[0].State.RedirectUris.Select(uri => uri.Value).Order().ShouldEqual(_cli.RedirectUris.Select(uri => uri.OriginalString).Order());
}
