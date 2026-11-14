// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_AuthenticatingLink;

public class when_serializing_tokens_for_a_grant_with_a_link : Specification
{
    readonly Guid _link = Guid.NewGuid();
    ServiceProvider _provider;
    ProcessSignInContext _context;
    JsonWebToken _jwt;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenIddict().AddServer(server =>
        {
            server.EnableDegradedMode()
                .SetTokenEndpointUris("connect/token")
                .AllowRefreshTokenFlow()
                .AddEphemeralEncryptionKey()
                .AddEphemeralSigningKey()
                .DisableAccessTokenEncryption()
                .AddEventHandler<ValidateTokenRequestContext>(handler => handler.UseInlineHandler(_ => default))
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
        AuthenticatingLink.Stamp(identity, _link);
        _context = new ProcessSignInContext(transaction) { Principal = new ClaimsPrincipal(identity), GenerateAccessToken = true, GenerateRefreshToken = true };
        _context.Principal.SetResources("direct-mcp");
        _context.Principal.SetScopes("offline_access");
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction) { RefreshTokenPrincipal = _context.Principal });
        await _provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(_context);
        _jwt = new JsonWebToken(_context.AccessToken);
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_finish_the_real_server_pipeline() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_keep_the_link_in_the_refresh_token_principal() => AuthenticatingLink.Of(_context.RefreshTokenPrincipal).ShouldEqual(_link);
    [Fact] void should_leave_the_link_out_of_the_access_token() => _jwt.Claims.Any(claim => claim.Type == AuthenticatingLink.ClaimType).ShouldBeFalse();
}
