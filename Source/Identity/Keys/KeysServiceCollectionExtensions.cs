// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.Keys;

/// <summary>Registers secret-store key custody and signing-key options.</summary>
internal static class KeysServiceCollectionExtensions
{
    /// <summary>Adds the secret-store-backed key ring when configured.</summary>
    /// <param name="services">The services.</param>
    /// <param name="startupOptions">The startup options.</param>
    /// <param name="developmentRoot">The pinned custody of local-development keys, or <see langword="null"/> for a deployed store.</param>
    internal static void AddSecretStoreKeys(this IServiceCollection services, IdentityOptions startupOptions, DevelopmentKeyStoreRoot? developmentRoot = null)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Cratis.Identity");
        if (!startupOptions.SecretStore.IsConfigured)
        {
            // Development only: ephemeral realm keys and the default Data Protection ring.
            return;
        }

        var deployed = developmentRoot is null;
        if (deployed)
        {
            // The secret store is the custody of the ring, so keys are neither generated nor wrapped by Identity.
            dataProtection.DisableAutomaticKeyGeneration();
        }
        else
        {
            // Never give Data Protection a pathname to re-resolve: use the child pinned during provisioning.
            services.AddOptions<KeyManagementOptions>().Configure<DevelopmentKeyStoreRoot>((options, root) =>
                options.XmlRepository = new DevelopmentXmlRepository(root));
            if (OperatingSystem.IsWindows())
            {
                dataProtection.ProtectKeysWithDpapi();
            }
        }

        services.AddSingleton(sp =>
        {
            var keys = new SecretStoreKeys(
                sp.GetRequiredService<IOptions<IdentityOptions>>().Value.SecretStore,
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<SecretStoreKeys>>(),
                deployed,
                deployed ? null : sp.GetRequiredService<DevelopmentKeyStoreRoot>());

            // The first read validates the store, so an invalid one stops the host when the service is first resolved.
            _ = keys.Current;
            return keys;
        });
        if (deployed)
        {
            services.AddSecretStoreKeyRing();
        }

        services.AddHostedService<KeyRingReloading>();
    }

    /// <summary>Adds signing-key options after OpenIddict has registered its credential ordering.</summary>
    /// <param name="services">The services.</param>
    internal static void AddSigningKeyOptions(this IServiceCollection services)
    {
        // Keep this after AddOpenIddict(): both configure and post-configure ordering are significant.
        services.AddSingleton<ConfigureServerKeys>();
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>, SecretStoreKeysChangeTokenSource>();
    }
}
