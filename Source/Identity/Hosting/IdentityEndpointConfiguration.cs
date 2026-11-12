// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.Hosting;

/// <summary>Shares the validated endpoint paths between registration and mapping.</summary>
internal static class IdentityEndpointConfiguration
{
    /// <summary>Registers the configured endpoint paths.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The validated endpoint options.</returns>
    internal static IdentityEndpointOptions AddIdentityEndpoints(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new IdentityEndpointOptions
        {
            RoutePrefix = configuration["Identity:Endpoints:RoutePrefix"] ?? string.Empty,
            EndSessionEnabled = !string.IsNullOrWhiteSpace(configuration["Identity:Issuer"])
        };
        options.Validate();
        services.AddSingleton(Options.Create(options));
        return options;
    }

    /// <summary>Checks mapping settings against the registered protocol paths.</summary>
    /// <param name="services">The host service provider.</param>
    /// <param name="configure">Optional mapping settings.</param>
    /// <returns>The shared endpoint options.</returns>
    /// <exception cref="InvalidOperationException">The prefix differs from registration or endpoints have already been mapped.</exception>
    internal static IdentityEndpointOptions ResolveIdentityEndpoints(this IServiceProvider services, Action<IdentityEndpointOptions>? configure)
    {
        var registered = services.GetService<IOptions<IdentityEndpointOptions>>()?.Value ?? new IdentityEndpointOptions();
        var mapped = new IdentityEndpointOptions { RoutePrefix = registered.RoutePrefix, MapSignOut = registered.MapSignOut };
        configure?.Invoke(mapped);
        mapped.Validate();
        if (!string.Equals(mapped.RoutePrefix.Value, registered.RoutePrefix.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Identity endpoint prefix '{mapped.RoutePrefix}' does not match the registered prefix '{registered.RoutePrefix}'. Configure Identity:Endpoints:RoutePrefix before AddCratisIdentity so OpenIddict and middleware use the same paths.");
        }

        if (registered.IsMapped)
        {
            throw new InvalidOperationException("Identity endpoints have already been mapped. Call MapCratisIdentity once per host; its sign-out setting cannot be changed by a later mapping.");
        }

        registered.MapSignOut = mapped.MapSignOut;
        registered.IsMapped = true;
        return registered;
    }
}
