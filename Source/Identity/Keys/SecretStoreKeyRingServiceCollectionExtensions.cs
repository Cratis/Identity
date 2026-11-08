// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Identity.Keys;

/// <summary>
/// Extension methods for serving the Data Protection key ring from the secret store.
/// </summary>
internal static class SecretStoreKeyRingServiceCollectionExtensions
{
    /// <summary>
    /// Uses the secret store as the read-only Data Protection key repository, and makes a reload invalidate the cached key ring.
    /// Call after <c language="text">AddDataProtection</c>; <see cref="SecretStoreKeys"/> must be registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddSecretStoreKeyRing(this IServiceCollection services)
    {
        // Since .NET 9 an expired key ring is served stale while it refreshes in the background; refresh in line so a reload takes effect on the next operation.
        AppContext.SetSwitch("Microsoft.AspNetCore.DataProtection.KeyManagement.DisableAsyncKeyRingUpdate", true);
        services.AddSingleton<SecretStoreXmlRepository>();
        services.AddOptions<KeyManagementOptions>().Configure<SecretStoreXmlRepository>((options, repository) => options.XmlRepository = repository);
        services.Replace(ServiceDescriptor.Singleton<IKeyManager>(sp => new SecretStoreKeyManager(
            ActivatorUtilities.CreateInstance<XmlKeyManager>(sp),
            sp.GetRequiredService<SecretStoreKeys>())));
        return services;
    }
}
