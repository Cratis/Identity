// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_sweeping_an_undeclared_legacy_client : given.a_sweeping_client_store
{
    void Establish()
    {
        var id = ObjectId.GenerateNewId();
        _applications[id] = new() { Id = id, ClientId = "undeclared-legacy-public-client", ClientType = OpenIddictConstants.ClientTypes.Public };
    }

    async Task Because() => await Reconcile();

    [Fact] void should_delete_unmarked_legacy_documents() => _applications.ShouldBeEmpty();
    [Fact] void should_not_invent_a_registration_or_retirement_for_a_legacy_document() => _store.Appended.ShouldBeEmpty();
}
