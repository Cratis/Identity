// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.DPoP;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DPoP;

public class when_attaching_confirmation : Specification
{
    ProcessSignInContext _context;
    OpenIddictResponse _response;

    async Task Because()
    {
        var binding = new DPoPBinding { Jkt = "synthetic-thumbprint" };
        var transaction = new OpenIddictServerTransaction { Logger = NullLogger.Instance, Options = new OpenIddictServerOptions(), Response = new OpenIddictResponse { AccessToken = "synthetic-access-token", TokenType = "Bearer" } };
        _context = new ProcessSignInContext(transaction)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity("test")),
            AccessTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test")),
            IdentityTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test")),
            RefreshTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"))
        };
        await new AttachDPoPConfirmation(binding).HandleAsync(_context);
        await new ApplyDPoPTokenResponse(binding).HandleAsync(new ApplyTokenResponseContext(transaction));
        _response = transaction.Response;
    }

    [Fact] void should_use_the_dpop_response_scheme() => _response.TokenType.ShouldEqual("DPoP");
    [Fact] void should_emit_a_json_object_claim() => _context.AccessTokenPrincipal.FindFirst("cnf").ValueType.ShouldEqual("JSON");
    [Fact] void should_carry_the_thumbprint() => JsonDocument.Parse(_context.AccessTokenPrincipal.GetClaim("cnf")).RootElement.GetProperty("jkt").GetString().ShouldEqual("synthetic-thumbprint");
    [Fact] void should_not_add_confirmation_to_the_id_token() => _context.IdentityTokenPrincipal.HasClaim(claim => claim.Type == "cnf").ShouldBeFalse();
    [Fact] void should_not_add_confirmation_to_the_refresh_token() => _context.RefreshTokenPrincipal.HasClaim(claim => claim.Type == "cnf").ShouldBeFalse();
    [Fact] void should_not_add_confirmation_to_the_shared_principal() => _context.Principal.HasClaim(claim => claim.Type == "cnf").ShouldBeFalse();
}
