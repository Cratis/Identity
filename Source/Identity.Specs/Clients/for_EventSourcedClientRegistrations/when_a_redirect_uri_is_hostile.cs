// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_a_redirect_uri_is_hostile : given.a_client_registration_store
{
    Exception _error;
    void Establish() => _confidential.RedirectUris.Add(new("https://user:password@x/callback?code=redeemable"));
    async Task Because() => _error = await Catch.Exception(async () => await Reconcile(_cli, _confidential));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidClientValue>();
    [Fact] void should_validate_every_descriptor_before_any_append() => _store.Appended.ShouldBeEmpty();
    [Fact] void should_not_materialize_anything() => _materialized.ShouldBeEmpty();
}
