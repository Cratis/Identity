// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

namespace Cratis.Identity.Server;

/// <summary>
/// Configures the hosted server's ingress and recovery readiness boundary.
/// </summary>
internal static class HostedSharedHosting
{
    /// <summary>Registers explicitly trusted ingress addresses without trusting arbitrary forwarded headers.</summary>
    /// <param name="builder">The host builder.</param>
    internal static void AddProxyHeaders(WebApplicationBuilder builder) => builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Keep the framework's loopback defaults. Only explicitly configured ingress addresses are added.
        foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    });

    /// <summary>Maps readiness to the recovery barrier without changing package liveness.</summary>
    /// <param name="app">The host application.</param>
    internal static void MapReadiness(WebApplication app) => app.MapHealthChecks("/readyz", new HealthCheckOptions
    {
        Predicate = registration => registration.Name == "identity-recovery"
    });
}
