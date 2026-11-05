// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.for_ConfigureServerKeys;

/// <summary>
/// A refresh token is encrypted by the encryption key in use when it is issued; it must still decrypt, with the options
/// OpenIddict validates it with, once a newer encryption key has taken over.
/// </summary>
public class when_decrypting_a_token_after_the_encryption_key_rotates : Specification
{
    string _directory;
    SecretStoreKeys _keys;
    ServiceProvider _provider;
    TokenValidationResult _result;

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
            server.SetTokenEndpointUris("connect/token").AllowRefreshTokenFlow();
            server.UseAspNetCore();
        });
        services.AddSingleton(_keys);
        services.AddSingleton<ConfigureServerKeys>();
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>, SecretStoreKeysChangeTokenSource>();
        _provider = services.BuildServiceProvider();

        var monitor = _provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>();
        var before = monitor.CurrentValue;
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://issuer.example",
            Audience = "api",
            SigningCredentials = before.SigningCredentials[0],
            EncryptingCredentials = before.EncryptionCredentials[0]
        });

        new StoreFiles()
            .Pair("signing", "b", KeyMaterial.Rsa(now.AddMinutes(-11), now.AddDays(30)))
            .Pair("encryption", "b", KeyMaterial.Rsa(now.AddMinutes(-11), now.AddDays(30)))
            .WriteTo(_directory);
        _keys.Reload();

        var after = monitor.CurrentValue;
        _result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "https://issuer.example",
            ValidAudience = "api",
            IssuerSigningKeys = after.TokenValidationParameters.IssuerSigningKeys,
            TokenDecryptionKeys = after.TokenValidationParameters.TokenDecryptionKeys
        });
    }

    void Destroy()
    {
        _provider.Dispose();
        _keys.Dispose();
        Directory.Delete(_directory, true);
    }

    [Fact] void should_validate_the_token() => _result.IsValid.ShouldBeTrue();
}
