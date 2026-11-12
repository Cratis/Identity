// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_KeysServiceCollectionExtensions;

public class when_oidc_issuance_is_disabled_with_an_ec_ring : for_SecretStoreKeys.given.a_secret_store_directory
{
    Exception? _disabled;
    Exception? _enabled;
    Exception? _withoutClients;

    void Establish() => new StoreFiles()
        .Pair("signing", "a", KeyMaterial.Ec(KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(30)))
        .Pair("encryption", "a", KeyMaterial.CurrentRsa())
        .DataProtection("a", KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(90))
        .WriteTo(_directory);

    void Because()
    {
        var options = new IdentityOptions { SecretStore = _options };
        options.OidcClients.Add(new OidcClientOptions { ClientId = "oidc-web" });
        _disabled = Resolve(options);
        options.Oidc.IssuanceEnabled = true;
        _enabled = Resolve(options);
        options.OidcClients.Clear();
        _withoutClients = Resolve(options);
    }

    [Fact] void should_not_require_rsa_when_issuance_is_disabled() => _disabled.ShouldBeNull();
    [Fact] void should_require_rsa_when_issuance_is_enabled_with_clients() => _enabled.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_not_require_rsa_without_oidc_clients() => _withoutClients.ShouldBeNull();

    Exception? Resolve(IdentityOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(_time);
        services.AddSingleton(Options.Create(options));
        services.AddSecretStoreKeys(options);
        using var provider = services.BuildServiceProvider();
        return Catch.Exception(() => provider.GetRequiredService<SecretStoreKeys>());
    }
}
