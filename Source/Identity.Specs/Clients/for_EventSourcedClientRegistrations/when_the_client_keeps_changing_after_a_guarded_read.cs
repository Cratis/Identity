// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_the_client_keeps_changing_after_a_guarded_read : given.a_client_registration_store
{
    Exception _error;

    void Establish()
    {
        _store.Outcomes.Enqueue(ClientRegistrationOutcome.Conflict);
        _store.Outcomes.Enqueue(ClientRegistrationOutcome.Conflict);
    }

    async Task Because() => _error = await Catch.Exception(async () => await Reconcile(_confidential));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_only_once() => _store.Attempts.ShouldEqual(2);
    [Fact] void should_not_materialize_uncommitted_configuration() => _materialized.ShouldBeEmpty();
}
