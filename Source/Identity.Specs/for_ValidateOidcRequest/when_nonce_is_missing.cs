// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ValidateOidcRequest;

public class when_nonce_is_missing : Specification
{
    ValidateAuthorizationRequestContext _context;
    ValidateOidcRequest _handler;

    void Establish()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        _handler = new(Options.Create(options));
        _context = new(new OpenIddictServerTransaction
        {
            Options = new(),
            Request = new OpenIddictRequest { ClientId = "oidc-web", Scope = "openid profile" },
            Response = new()
        });
    }

    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_before_login_or_consent() => _context.IsRejected.ShouldBeTrue();
    [Fact] void should_report_invalid_request() => _context.Error.ShouldEqual("invalid_request");
}
