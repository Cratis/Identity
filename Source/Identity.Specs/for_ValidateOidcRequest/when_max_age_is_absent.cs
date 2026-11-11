// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ValidateOidcRequest;

public class when_max_age_is_absent : Specification
{
    ValidateAuthorizationRequestContext _context;

    async Task Because()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        _context = new(new OpenIddictServerTransaction
        {
            Options = new(),
            Request = new OpenIddictRequest { ClientId = "oidc-web", Scope = "openid", Nonce = "request-nonce" },
            Response = new()
        }) { ClientId = "oidc-web" };
        await new ValidateOidcRequest(Options.Create(options)).HandleAsync(_context);
    }

    [Fact] void should_leave_a_valid_identity_request_unchanged() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_not_supply_an_error() => _context.Error.ShouldBeNull();
}
