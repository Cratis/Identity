// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.Keys;

/// <summary>Registers secret-store key custody and signing-key options.</summary>
internal static class KeysServiceCollectionExtensions
{
    /// <summary>Adds the secret-store-backed key ring when configured.</summary>
    /// <param name="services">The services.</param>
    /// <param name="startupOptions">The startup options.</param>
    /// <param name="developmentDataProtectionPath">The Data Protection key directory of the local-development profile, or <see langword="null"/> for the deployed ring in the secret store.</param>
    internal static void AddSecretStoreKeys(this IServiceCollection services, IdentityOptions startupOptions, string? developmentDataProtectionPath = null)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Cratis.Identity");
        if (!startupOptions.SecretStore.IsConfigured)
        {
            // Development only: ephemeral realm keys and the default Data Protection ring.
            return;
        }

        var deployed = developmentDataProtectionPath is null;
        if (deployed)
        {
            // The secret store is the custody of the ring, so keys are neither generated nor wrapped by Identity.
            dataProtection.DisableAutomaticKeyGeneration();
        }
        else
        {
            // The local-development profile lets ASP.NET Core generate its own ring, next to the development keys.
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(developmentDataProtectionPath!));
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
                deployed);

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
