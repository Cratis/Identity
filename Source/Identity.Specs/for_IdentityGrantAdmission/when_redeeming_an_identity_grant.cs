// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_IdentityGrantAdmission;

public class when_redeeming_an_identity_grant : for_OidcIdentity.given.an_identity_grant
{
    ClaimsPrincipal _grant;
    IdentityOptions _options;
    OpenIddictRequest _request;
    OpenIddictRequest _widened;
    OpenIddictRequest _resource;

    void Establish()
    {
        var identity = OidcIdentity.Create(new(_account, AuthTime, SessionId), ClientId, new IdentityOnlyGrant(["openid", "profile"]), "example-nonce");
        identity.SetPresenters(ClientId);
        _grant = new(identity);
        _options = new IdentityOptions();
        _options.Oidc.IssuanceEnabled = true;
        _options.OidcClients.Add(new OidcClientOptions { ClientId = ClientId, Scopes = ["openid", "profile"] });
        _request = new() { ClientId = ClientId };
        _widened = new() { ClientId = ClientId, Scope = "openid profile offline_access" };
        _resource = new() { ClientId = ClientId };
        _resource.SetParameter("resource", "https://resource.example/");
    }

    [Fact] void should_accept_the_original_binding() => IdentityGrantAdmission.CanRedeem(_grant, _request, _options).ShouldBeTrue();
    [Fact] void should_refuse_a_scope_upgrade() => IdentityGrantAdmission.CanRedeem(_grant, _widened, _options).ShouldBeFalse();
    [Fact] void should_refuse_a_resource_upgrade() => IdentityGrantAdmission.CanRedeem(_grant, _resource, _options).ShouldBeFalse();
    [Fact] void should_refuse_a_different_client() => IdentityGrantAdmission.CanRedeem(_grant, new OpenIddictRequest { ClientId = "other-client" }, _options).ShouldBeFalse();
    [Fact] void should_allow_narrowing_without_upgrading_profile() => IdentityGrantAdmission.CanRedeem(_grant, new OpenIddictRequest { ClientId = ClientId, Scope = "openid" }, _options).ShouldBeTrue();
}
