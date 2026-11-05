// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_client_changes_after_a_guarded_read : given.a_client_registration_store
{
    void Establish() => _store.Outcomes.Enqueue(ClientRegistrationOutcome.Conflict);
    async Task Because() => await Reconcile(_confidential);

    [Fact] void should_retry_the_diff_once() => _store.Attempts.ShouldEqual(2);
    [Fact] void should_materialize_only_after_success() => _materialized.Count.ShouldEqual(1);
}
