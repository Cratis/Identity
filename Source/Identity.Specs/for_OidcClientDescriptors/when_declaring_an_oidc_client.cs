// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_OidcClientDescriptors;

public class when_declaring_an_oidc_client : given.an_oidc_client
{
    void Because() => Declare();

    [Fact] void should_be_confidential() => descriptor.ClientType.ShouldEqual(ClientTypes.Confidential);
    [Fact] void should_be_a_web_application() => descriptor.ApplicationType.ShouldEqual(ApplicationTypes.Web);
    [Fact] void should_use_implicit_consent() => descriptor.ConsentType.ShouldEqual(ConsentTypes.Implicit);
    [Fact] void should_carry_the_secret() => descriptor.ClientSecret.ShouldEqual(client.ClientSecret);
    [Fact] void should_carry_the_display_name() => descriptor.DisplayName.ShouldEqual(client.DisplayName);
    [Fact] void should_require_pkce() => descriptor.Requirements.ShouldContainOnly(Requirements.Features.ProofKeyForCodeExchange);
    [Fact] void should_allow_only_the_configured_scopes() => descriptor.Permissions.Where(_ => _.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(Permissions.Prefixes.Scope + Scopes.OpenId, Permissions.Prefixes.Scope + Scopes.Profile);
    [Fact] void should_allow_only_the_code_flow_and_required_endpoints() => descriptor.Permissions.Where(_ => !_.StartsWith(Permissions.Prefixes.Scope)).ShouldContainOnly(Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.EndSession, Permissions.Endpoints.Revocation, Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code);
    [Fact] void should_register_the_redirect() => descriptor.RedirectUris.ShouldContainOnly(new Uri(client.RedirectUris.Single()));
    [Fact] void should_register_the_post_logout_redirect() => descriptor.PostLogoutRedirectUris.ShouldContainOnly(new Uri(client.PostLogoutRedirectUris.Single()));
    [Fact] void should_satisfy_the_registration_rules() => ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]).IsValid.ShouldBeTrue();
    [Fact] void should_keep_the_cli() => all.Select(_ => _.ClientId).ShouldContainOnly(ClientDescriptors.CratisCli, client.ClientId);
    [Fact] void should_not_give_the_cli_identity_scopes() => all.Single(_ => _.ClientId == ClientDescriptors.CratisCli).Permissions.ShouldNotContain(Permissions.Prefixes.Scope + Scopes.OpenId);
}
