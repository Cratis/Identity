// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_restarting_with_unchanged_configuration : given.a_client_registration_store
{
    async Task Establish()
    {
        await Seed(_cli);
        await Seed(_confidential);
    }

    async Task Because() => await Reconcile(_cli, _confidential);

    [Fact] void should_append_nothing() => _store.Appended.ShouldBeEmpty();
    [Fact] void should_rebuild_the_operational_copies() => _materialized.Count.ShouldEqual(2);
}
