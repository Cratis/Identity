// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.given;

/// <summary>
/// The one place that knows which implementation serves clients and resources, so the catalog specs
/// keep the same assertions whichever source (configuration today, read models later) sits behind the seam.
/// </summary>
internal static class CatalogServices
{
    /// <summary>
    /// Builds the services that serve clients and resources for the given options.
    /// </summary>
    /// <param name="options">The configured realm policy.</param>
    /// <returns>The <see cref="ServiceProvider"/>; the caller disposes it.</returns>
    public static ServiceProvider Build(IdentityOptions options)
    {
        var asOptions = Options.Create(options);
        var resources = new ResourceCatalog(asOptions);
        var services = new ServiceCollection();
        services.AddSingleton<IResourceCatalog>(resources);
        services.AddSingleton(new ClientDescriptors(resources, asOptions));
        services.AddOpenIddict().AddCore(core => core.UseConfigurationStores());
        return services.BuildServiceProvider();
    }
}
