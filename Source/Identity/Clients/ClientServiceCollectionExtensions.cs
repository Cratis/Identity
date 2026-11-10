// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registering;

namespace Cratis.Identity.Clients;

/// <summary>Isolated registration for Internal client administration.</summary>
internal static class ClientServiceCollectionExtensions
{
    /// <summary>Registers the server-held producer and registration reads; reducers and constraints use Chronicle discovery.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Protected server configuration.</param>
    internal static void AddClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClientSecretOptions>()
            .Bind(configuration.GetSection("Identity:ClientSecrets"))
            .Validate(options => options.HasValidKey(), "invalid_client_secret_key_configuration")
            .Validate(options => options.Revision > 0 && options.MaxOverlap >= TimeSpan.Zero, "invalid_client_secret_policy_configuration")
            .ValidateOnStart();
        services.AddSingleton<ClientSecretDigester>();
        services.AddScoped<ClientRegistrationReads>();
    }
}
