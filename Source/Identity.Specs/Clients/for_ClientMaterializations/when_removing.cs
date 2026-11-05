// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Storage;

namespace Cratis.Identity.Clients.for_ClientMaterializations;

public class when_removing : given.a_materialization
{
    async Task Because() => await _sink.Remove(_state.ClientRegistrationId, CancellationToken.None);

    [Fact] void should_delete_by_the_stable_object_id() => _deleted["_id"].AsObjectId.ShouldEqual(ClientMaterializations.ApplicationId(_state.ClientRegistrationId));
    [Fact] void should_not_upsert() => _operations.ShouldEqual(new[] { "remove" });
}
