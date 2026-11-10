// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registering;

namespace Cratis.Identity.Clients;

/// <summary>Isolated registration for Internal client administration.</summary>
internal static class ClientServiceCollectionExtensions
{
    /// <summary>Adds configured client descriptors.</summary>
    /// <param name="services">The service collection.</param>
    internal static void AddClients(this IServiceCollection services) => services.AddSingleton<ClientDescriptors>();

    /// <summary>Registers the server-held producer and registration reads; reducers and constraints use Chronicle discovery.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Protected server configuration.</param>
    internal static void AddClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClientSecretOptions>()
            .Bind(configuration.GetSection("Identity:ClientSecrets"))
            .Validate(options => string.IsNullOrEmpty(options.Key) || options.HasValidKey(), "invalid_client_secret_key_configuration")
            .Validate(options => HasValidRevision(options) && options.MaxOverlap >= TimeSpan.Zero, "invalid_client_secret_policy_configuration")
            .ValidateOnStart();
        services.AddSingleton<ClientSecretDigester>();
        services.AddScoped<ClientRegistrationReads>();
    }

    static bool HasValidRevision(ClientSecretOptions options) => string.IsNullOrEmpty(options.Key) || options.Revision > 0;
}
