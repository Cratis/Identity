// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Registers a dedicated credential-free transport; no retry/resilience handler is installed.</summary>
internal static class MetadataDocumentServiceCollectionExtensions
{
    const string ClientName = "Identity.Cimd";

    /// <summary>Registers the bounded metadata resolver. Disabled policy never invokes its transport.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="enabled">Whether lookup is enabled at startup.</param>
    /// <returns>The services for continuation.</returns>
    public static IServiceCollection AddClientIdMetadataDocuments(this IServiceCollection services, bool enabled)
    {
        if (!enabled) return services;
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<IdentityOptions>>().Value.Clients.MetadataDocuments);
        services.TryAddSingleton<IHostResolver, HostResolver>();
        services.TryAddSingleton<IAddressConnector, AddressConnector>();
        services.AddSingleton<GuardedConnect>();
        services.AddSingleton<MetadataDocumentParser>();
        services.AddHttpClient(ClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(provider => provider.GetRequiredService<GuardedConnect>().CreateHandler(provider.GetRequiredService<ClientIdMetadataOptions>().ConnectTimeout))
            .RemoveAllLoggers();
        services.AddSingleton<IMetadataDocumentFetcher>(provider => new ClientIdMetadataDocumentFetcher(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName),
            provider.GetRequiredService<MetadataDocumentParser>(),
            provider.GetRequiredService<ClientIdMetadataOptions>()));
        services.AddSingleton<IClientIdMetadataDocuments>(provider => new ClientIdMetadataDocuments(
            provider.GetRequiredService<IMetadataDocumentFetcher>(),
            provider.GetRequiredService<ClientIdMetadataOptions>(),
            provider.GetRequiredService<IResourceCatalog>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<ClientIdMetadataDocuments>>()));
        return services;
    }
}
