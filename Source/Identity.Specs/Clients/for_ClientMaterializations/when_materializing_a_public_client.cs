// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.for_ClientMaterializations;

public class when_materializing_a_public_client : given.a_materialization
{
    void Establish() => _policy.ClientType = OpenIddictConstants.ClientTypes.Public;
    async Task Because() => await _sink.Upsert(_state, _policy, CancellationToken.None);

    [Fact] void should_never_materialize_a_secret_for_a_public_client() => _document.ClientSecret.ShouldBeNull();
}
