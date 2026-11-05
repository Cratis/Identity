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
/// ES256 must really work: sign through the credential OpenIddict would use, publish the key as a JWK and validate with it.
/// </summary>
public class when_signing_with_an_ec_key : Specification
{
    string _directory;
    SecretStoreKeys _keys;
    ServiceProvider _provider;
    OpenIddictServerOptions _options;
    string _kid;
    string _jwtAlgorithm;
    string _jwtKid;
    TokenValidationResult _validated;
    JsonWebKey _jwk;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"identity-specs-{Guid.NewGuid():N}");
        var now = DateTimeOffset.UtcNow;
        var ec = KeyMaterial.Ec(now.AddDays(-1), now.AddDays(30));
        _kid = KeyMaterial.KeyThumbprint(ec);
        new StoreFiles()
            .Pair("signing", "ec", ec)
            .Pair("encryption", "a", KeyMaterial.Rsa(now.AddDays(-1), now.AddDays(30)))
            .DataProtection("a", now.AddDays(-1), now.AddDays(90))
            .WriteTo(_directory);
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _directory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenIddict().AddServer(server =>
        {
            server.SetAuthorizationEndpointUris("connect/authorize").SetTokenEndpointUris("connect/token").AllowAuthorizationCodeFlow();
            server.UseAspNetCore();
        });
        services.AddSingleton(_keys);
        services.AddSingleton<ConfigureServerKeys>();
        services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        _provider = services.BuildServiceProvider();
        _options = _provider.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;

        var credentials = _options.SigningCredentials[0];
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://issuer",
            Audience = "aud",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = credentials
        });
        var parsed = new JsonWebToken(token);
        _jwtAlgorithm = parsed.Alg;
        _jwtKid = parsed.Kid;

        // What OpenIddict publishes at the JWKS endpoint, then validated the way a resource server does.
        _jwk = JsonWebKeyConverter.ConvertFromSecurityKey(credentials.Key);
        _validated = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "https://issuer",
            ValidAudience = "aud",
            IssuerSigningKeys = [_jwk]
        });
    }

    void Destroy()
    {
        _provider.Dispose();
        _keys.Dispose();
        Directory.Delete(_directory, true);
    }

    [Fact] void should_sign_with_es256() => _jwtAlgorithm.ShouldEqual(SecurityAlgorithms.EcdsaSha256);
    [Fact] void should_put_the_kid_in_the_header() => _jwtKid.ShouldEqual(_kid);
    [Fact] void should_publish_an_ec_jwk_with_the_kid() => (_jwk.Kty, _jwk.Crv, _jwk.Kid).ShouldEqual(("EC", "P-256", _kid));
    [Fact] void should_validate_with_the_published_key() => _validated.IsValid.ShouldBeTrue();
}
