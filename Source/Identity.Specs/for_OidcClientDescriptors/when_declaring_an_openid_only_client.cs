// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcClientDescriptors;

public class when_declaring_an_openid_only_client : given.an_oidc_client
{
    void Establish() => client.Scopes = [Scopes.OpenId];
    void Because() => Declare();

    [Fact] void should_allow_only_openid() => descriptor.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(Permissions.Prefixes.Scope + Scopes.OpenId);
    [Fact] void should_not_allow_profile() => descriptor.Permissions.ShouldNotContain(Permissions.Prefixes.Scope + Scopes.Profile);
    [Fact] void should_not_allow_refresh() => descriptor.Permissions.ShouldNotContain(Permissions.GrantTypes.RefreshToken);
}
