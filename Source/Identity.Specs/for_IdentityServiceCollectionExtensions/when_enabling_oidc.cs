// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_enabling_oidc : given.an_oidc_server
{
    [Fact] void should_register_openid_and_profile() => _server.Scopes.ShouldContain("openid");
    [Fact] void should_register_profile() => _server.Scopes.ShouldContain("profile");
    [Fact] void should_configure_the_userinfo_endpoint() => _server.UserInfoEndpointUris.Select(uri => uri.OriginalString).ShouldContainOnly("connect/userinfo");
    [Fact] void should_apply_the_configured_id_token_lifetime() => _server.IdentityTokenLifetime.ShouldEqual(TimeSpan.FromMinutes(4));
    [Fact] void should_order_id_signing_before_serialization() => _server.Handlers.Exists(handler => handler.ServiceDescriptor.ServiceType == typeof(IdentityTokenSigning)).ShouldBeTrue();
}
