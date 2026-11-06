// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.for_ClientMaterializations;

public class when_a_conflicting_document_exists : given.a_materialization
{
    async Task Because() => await _sink.Upsert(_state, _policy, CancellationToken.None);

    [Fact] void should_delete_conflicts_before_the_upsert() => _operations.ShouldEqual(new[] { "delete-conflicting", "upsert" });
    [Fact] void should_delete_only_the_same_client_id() => _deleted["client_id"].AsString.ShouldEqual(_state.ClientId!.Value);
    [Fact] void should_preserve_the_same_registration_id() => _deleted["_id"]["$ne"].AsObjectId.ShouldEqual(_document.Id);
}
