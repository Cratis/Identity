// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcClientDescriptors;

public class when_declaring_a_client_with_offline_access : given.an_oidc_client
{
    void Establish() => client.Scopes = [Scopes.OpenId, Scopes.OfflineAccess];
    void Because() => Declare();

    [Fact] void should_allow_refresh() => descriptor.Permissions.ShouldContain(Permissions.GrantTypes.RefreshToken);
    [Fact] void should_allow_only_the_configured_scopes() => descriptor.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(Permissions.Prefixes.Scope + Scopes.OpenId, Permissions.Prefixes.Scope + Scopes.OfflineAccess);
    [Fact] void should_not_allow_profile() => descriptor.Permissions.ShouldNotContain(Permissions.Prefixes.Scope + Scopes.Profile);
}
