// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_leaving_oidc_disabled : given.an_oidc_server
{
    protected override bool IssuanceEnabled => false;

    [Fact] void should_not_register_openid() => _server.Scopes.ShouldNotContain("openid");
    [Fact] void should_not_register_profile() => _server.Scopes.ShouldNotContain("profile");
    [Fact] void should_not_register_userinfo() => _server.UserInfoEndpointUris.ShouldBeEmpty();
    [Fact] void should_not_change_oauth_signing_handlers() => _server.Handlers.Any(handler => handler.ServiceDescriptor.ServiceType == typeof(IdentityTokenSigning)).ShouldBeFalse();
}
