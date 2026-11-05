// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_retired_document_removal_failed : given.a_sweeping_client_store
{
    Exception _error;

    async Task Establish()
    {
        await Reconcile(_confidential);
        var id = _materialized.Single().State.ClientRegistrationId;
        _materializations.Remove(default!, default).ReturnsForAnyArgs(_ => Task.FromException(new IOException("Operational removal failed.")));
        _error = await Catch.Exception(async () => await Reconcile());
        await Refresh(id);
    }

    async Task Because() => await Reconcile();

    [Fact] void should_have_committed_the_retirement_once() => _store.Appended.OfType<ClientRetired>().Count().ShouldEqual(1);
    [Fact] void should_have_failed_the_original_remove() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_delete_the_surviving_document_on_the_next_reconcile() => _applications.ShouldBeEmpty();
}
