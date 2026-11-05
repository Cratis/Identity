// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.for_ConfigureServerKeys;

/// <summary>
/// Confirms against the real OpenIddict options pipeline what the key rotation design rests on: the first
/// credential signs and encrypts, every signing credential is published, and OpenIddict's own sorting runs before
/// the post-configuration registered after it.
/// </summary>
public class when_building_server_options : Specification
{
    string _directory;
    SecretStoreKeys _keys;
    ServiceProvider _provider;
    OpenIddictServerOptions _options;
    OpenIddictServerOptions _rebuilt;
    string _oldKid;
    string _newKid;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _directory = Path.Combine(Path.GetTempPath(), $"identity-specs-{Guid.NewGuid():N}");

        // The old key signs; the pre-published one expires later, which OpenIddict's own sorting prefers.
        var old = KeyMaterial.Rsa(now.AddDays(-30), now.AddDays(30));
        var successor = KeyMaterial.Rsa(now.AddMinutes(-2), now.AddDays(60));
        _oldKid = KeyMaterial.KeyThumbprint(old);
        _newKid = KeyMaterial.KeyThumbprint(successor);
        new StoreFiles()
            .Pair("signing", "old", old)
            .Pair("signing", "new", successor)
            .Pair("encryption", "old", KeyMaterial.Rsa(now.AddDays(-30), now.AddDays(30)))
            .DataProtection("a", now.AddDays(-1), now.AddDays(90))
            .WriteTo(_directory);

        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _directory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenIddict().AddServer(server =>
        {
            server.SetAuthorizationEndpointUris("connect/authorize").SetTokenEndpointUris("connect/token").AllowAuthorizationCodeFlow();
            server.UseAspNetCore();
            server.AddEventHandler(RejectExpiredTokenEntries.Descriptor);
        });
        services.AddSingleton(_keys);
        services.AddSingleton<ConfigureServerKeys>();
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>, SecretStoreKeysChangeTokenSource>();
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        var monitor = _provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>();
        _options = monitor.CurrentValue;

        // A third key is published after the options were built, and the options are rebuilt through the change token.
        var third = KeyMaterial.Rsa(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
        new StoreFiles().Pair("signing", "third", third).WriteTo(_directory);
        _keys.Reload();
        _rebuilt = monitor.CurrentValue;
    }

    void Destroy()
    {
        _provider.Dispose();
        _keys.Dispose();
        Directory.Delete(_directory, true);
    }

    [Fact] void should_put_the_current_signer_first() => _options.SigningCredentials[0].Key.KeyId.ShouldEqual(_oldKid);
    [Fact] void should_publish_every_signing_key() => _options.SigningCredentials.Select(_ => _.Key.KeyId).ShouldContainOnly(_oldKid, _newKid);
    [Fact] void should_sign_rsa_keys_with_rs256() => _options.SigningCredentials.TrueForAll(_ => _.Algorithm == SecurityAlgorithms.RsaSha256).ShouldBeTrue();
    [Fact] void should_encrypt_with_the_eligible_key() => _options.EncryptionCredentials[0].Key.KeyId.ShouldEqual(_keys.Current.Encrypter.Kid);
    [Fact] void should_decrypt_with_every_encryption_key() => _options.TokenValidationParameters.TokenDecryptionKeys.Count().ShouldEqual(1);
    [Fact] void should_validate_with_every_signing_key() => _options.TokenValidationParameters.IssuerSigningKeys.Count().ShouldEqual(2);
    [Fact] void should_keep_the_custom_handlers() => _options.Handlers.Exists(_ => _.ContextType == typeof(OpenIddictServerEvents.ValidateTokenContext) && _.ServiceDescriptor.ServiceType.Name.Contains("RejectExpiredTokenEntries")).ShouldBeTrue();
    [Fact] void should_rebuild_the_options_when_the_keys_change() => _rebuilt.ShouldNotBeSame(_options);
    [Fact] void should_publish_the_new_key_after_the_rebuild() => _rebuilt.SigningCredentials.Count.ShouldEqual(3);
    [Fact] void should_keep_the_signer_after_the_rebuild() => _rebuilt.SigningCredentials[0].Key.KeyId.ShouldEqual(_oldKid);
    [Fact] void should_keep_the_custom_handlers_after_the_rebuild() => _rebuilt.Handlers.Count.ShouldEqual(_options.Handlers.Count);
}
