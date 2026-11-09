// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.DPoP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DPoP;

public class when_serializing_a_bound_access_token : Specification
{
    ServiceProvider _provider;
    ProcessSignInContext _context;
    JsonWebToken _jwt;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new DPoPBinding { Jkt = "synthetic-thumbprint" });
        services.AddOpenIddict().AddServer(server =>
        {
            server.EnableDegradedMode()
                .SetTokenEndpointUris("connect/token")
                .AllowRefreshTokenFlow()
                .AddEphemeralEncryptionKey()
                .AddEphemeralSigningKey()
                .DisableAccessTokenEncryption()
                .AddEventHandler<ValidateTokenRequestContext>(handler => handler.UseInlineHandler(_ => default))
                .AddEventHandler(AttachDPoPConfirmation.Descriptor)
                .AddEventHandler(ApplyDPoPTokenResponse.Descriptor)
                .AddEventHandler<ApplyTokenResponseContext>(handler => handler.SetOrder(int.MaxValue).UseInlineHandler(context =>
                {
                    // A unit-level transport sink: no HTTP server or external dependencies.
                    context.HandleRequest();
                    return default;
                }));
            server.UseAspNetCore();
        });
        _provider = services.BuildServiceProvider();
        var transaction = new OpenIddictServerTransaction
        {
            Logger = NullLogger.Instance,
            Options = _provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue,
            Request = new OpenIddictRequest { GrantType = OpenIddictConstants.GrantTypes.RefreshToken, ClientId = "cratis-cli" },
            Response = new OpenIddictResponse(),
            EndpointType = OpenIddictServerEndpointType.Token,
            BaseUri = new Uri("https://identity.example/"),
            RequestUri = new Uri("https://identity.example/connect/token")
        };
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("sub", "synthetic-subject"));
        _context = new ProcessSignInContext(transaction) { Principal = new ClaimsPrincipal(identity), GenerateAccessToken = true, GenerateRefreshToken = true };
        _context.Principal.SetResources("direct-api");
        _context.Principal.SetScopes("offline_access");
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction) { RefreshTokenPrincipal = _context.Principal });
        await _provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(_context);
        _jwt = new JsonWebToken(_context.AccessToken);
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_finish_the_real_server_pipeline() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_apply_the_dpop_response_scheme() => _context.Response.TokenType.ShouldEqual("DPoP");
    [Fact] void should_keep_the_access_jwt_type() => _jwt.Typ.ShouldEqual("at+jwt");
    [Fact] void should_serialize_confirmation_as_an_object() => _jwt.GetPayloadValue<JsonElement>("cnf").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_carry_the_verified_thumbprint() => _jwt.GetPayloadValue<JsonElement>("cnf").GetProperty("jkt").GetString().ShouldEqual("synthetic-thumbprint");
    [Fact] void should_not_bind_the_refresh_token_payload() => _context.RefreshTokenPrincipal.HasClaim(claim => claim.Type == "cnf").ShouldBeFalse();
}
