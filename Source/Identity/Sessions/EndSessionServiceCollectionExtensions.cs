// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.Sessions;

/// <summary>Registers the browser end-session boundary and its validated hint limits.</summary>
internal static class EndSessionServiceCollectionExtensions
{
    /// <summary>Adds end-session services.</summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The Identity configuration section.</param>
    internal static void AddEndSession(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EndSessionOptions>()
            .Bind(configuration.GetSection(nameof(IdentityOptions.Sessions)))
            .Validate(options => options.EndSessionHintMaxAge > TimeSpan.Zero, "Identity:Sessions:EndSessionHintMaxAge must be positive.")
            .ValidateOnStart();
        services.AddScoped<EndSessionHintValidator>();
        services.AddScoped<EndSessionEndpoint>();
    }
}
