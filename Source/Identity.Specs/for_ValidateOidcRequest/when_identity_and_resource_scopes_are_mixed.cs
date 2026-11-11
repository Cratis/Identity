// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ValidateOidcRequest;

public class when_identity_and_resource_scopes_are_mixed : Specification
{
    ValidateAuthorizationRequestContext _mixed;
    ValidateAuthorizationRequestContext _resourceOnly;

    async Task Because()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        var handler = new ValidateOidcRequest(Options.Create(options));
        _mixed = Context("openid direct:read");
        _resourceOnly = Context("direct:read");
        await handler.HandleAsync(_mixed);
        await handler.HandleAsync(_resourceOnly);
    }

    [Fact] void should_refuse_before_login_or_consent() => _mixed.IsRejected.ShouldBeTrue();
    [Fact] void should_report_invalid_scope() => _mixed.Error.ShouldEqual("invalid_scope");
    [Fact] void should_not_change_resource_only_requests() => _resourceOnly.IsRejected.ShouldBeFalse();

    static ValidateAuthorizationRequestContext Context(string scope) => new(new OpenIddictServerTransaction
    {
        Options = new(),
        Request = new OpenIddictRequest { ClientId = "oidc-web", Scope = scope, Nonce = "request-nonce" },
        Response = new()
    }) { ClientId = "oidc-web" };
}
