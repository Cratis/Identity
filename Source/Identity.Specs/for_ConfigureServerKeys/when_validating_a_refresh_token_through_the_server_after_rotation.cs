// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ConfigureServerKeys;

/// <summary>
/// Issues a refresh token through the OpenIddict server pipeline, rotates the keys through a reload, and validates the token
/// through the same pipeline with a new transaction, as the host does for the next request.
/// </summary>
public class when_validating_a_refresh_token_through_the_server_after_rotation : Specification
{
    string _directory;
    SecretStoreKeys _keys;
    ServiceProvider _provider;
    ValidateTokenContext _context;
    string _issueError;

    async Task Because()
    {
        var now = DateTimeOffset.UtcNow;
        _directory = Path.Combine(Path.GetTempPath(), $"identity-specs-{Guid.NewGuid():N}");
        new StoreFiles()
            .Pair("signing", "a", KeyMaterial.Rsa(now.AddHours(-2), now.AddDays(30)))
            .Pair("encryption", "a", KeyMaterial.Rsa(now.AddHours(-2), now.AddDays(30)))
            .DataProtection("a", now.AddDays(-1), now.AddDays(90))
            .WriteTo(_directory);
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _directory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenIddict().AddServer(server =>
        {
            server.EnableDegradedMode()
                .SetTokenEndpointUris("connect/token")
                .AllowRefreshTokenFlow()
                .AddEventHandler<ValidateTokenRequestContext>(handler => handler.UseInlineHandler(_ => default));
            server.UseAspNetCore();
        });
        services.AddSingleton(_keys);
        services.AddSingleton<ConfigureServerKeys>();
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>, SecretStoreKeysChangeTokenSource>();
        _provider = services.BuildServiceProvider();

        var token = await Issue();

        new StoreFiles()
            .Pair("signing", "b", KeyMaterial.Rsa(now.AddMinutes(-11), now.AddDays(30)))
            .Pair("encryption", "b", KeyMaterial.Rsa(now.AddMinutes(-11), now.AddDays(30)))
            .WriteTo(_directory);
        _keys.Reload();

        var factory = _provider.GetRequiredService<IOpenIddictServerFactory>();
        var dispatcher = _provider.GetRequiredService<IOpenIddictServerDispatcher>();
        var transaction = await factory.CreateTransactionAsync();
        _context = new ValidateTokenContext(transaction) { Token = token };
        _context.ValidTokenTypes.Add(TokenTypeIdentifiers.RefreshToken);
        await dispatcher.DispatchAsync(_context);
    }

    void Destroy()
    {
        _provider.Dispose();
        _keys.Dispose();
        Directory.Delete(_directory, true);
    }

    [Fact] void should_have_issued_the_token() => _issueError.ShouldBeNull();
    [Fact] void should_accept_the_token() => (_context.Error is null ? null : $"{_context.Error}: {_context.ErrorDescription} {_context.TokenValidationResult?.Exception}").ShouldBeNull();
    [Fact] void should_resolve_the_principal() => _context.Principal.ShouldNotBeNull();

    async Task<string> Issue()
    {
        var factory = _provider.GetRequiredService<IOpenIddictServerFactory>();
        var dispatcher = _provider.GetRequiredService<IOpenIddictServerDispatcher>();
        var transaction = await factory.CreateTransactionAsync();
        var identity = new ClaimsIdentity("spec");
        identity.AddClaim(new Claim(Claims.Subject, "account-1"));
        var context = new GenerateTokenContext(transaction)
        {
            Principal = new ClaimsPrincipal(identity),
            TokenType = TokenTypeIdentifiers.RefreshToken,
            TokenFormat = "urn:openiddict:params:oauth:token-format:jwt"
        };
        await dispatcher.DispatchAsync(context);
        _issueError = context.Error ?? context.ErrorDescription;
        return context.Token!;
    }
}
