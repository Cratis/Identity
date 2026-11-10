// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_IntrospectionCredentialClaims.given;

public class a_validated_access_token : Specification
{
    protected const string AuthorizationId = "authorization-1";
    private protected RecoveryGate _gate;
    protected HandleIntrospectionRequestContext _context;
    private protected IntrospectionCredentialClaims _handler;
    private protected RefuseIssuanceDuringRecovery _validation;

    async Task Establish()
    {
        _gate = new();
        _gate.ObserveEpoch(7);
        _gate.Open(true);
        var principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        principal.SetAuthorizationId(AuthorizationId);
        principal.SetTokenType(TokenTypeIdentifiers.AccessToken);
        principal.SetClaim(Claims.JwtId, "token-id-not-a-grant");
        principal.SetClaim(Claims.ClientId, "cratis-cli");
        principal.SetClaim(IdentityClaims.TenantId, "token-tenant");
        principal.SetAudiences("direct-api");
        principal.SetScopes("direct:read", "offline_access");
        _context = new(new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Introspection,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions { Issuer = new Uri("https://identity.example.test/") },
            Request = new OpenIddictRequest { ClientId = "direct-api" }
        })
        {
            GenericTokenPrincipal = principal
        };
        _context.Request["tid"] = "request-tenant";
        _context.Request["audience"] = "request-audience";
        _handler = new(_gate);
        var log = Substitute.For<IRecoveryLog>();
        log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(_ => _gate.Epoch);
        _validation = new RefuseIssuanceDuringRecovery(_gate, log, Substitute.For<IOpenIddictTokenManager>(), Substitute.For<IOpenIddictAuthorizationManager>());
        await _validation.HandleAsync(new ValidateTokenContext(_context.Transaction));
    }

    protected async Task Populate()
    {
        await new OpenIddictServerHandlers.Introspection.AttachMetadataClaims().HandleAsync(_context);
        await _handler.HandleAsync(_context);
    }
}
