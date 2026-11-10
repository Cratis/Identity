// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device.Operational;
using OpenIddict.Server;

namespace Cratis.Identity.Device;

/// <summary>Registers opt-in device grants and their shared token admission helper.</summary>
internal static class DeviceServiceCollectionExtensions
{
    /// <summary>Adds device grant services when enabled.</summary>
    /// <param name="services">The services.</param>
    /// <param name="enabled">Whether device grants are enabled.</param>
    internal static void AddDeviceGrant(this IServiceCollection services, bool enabled)
    {
        // TokenEndpoint shares this admission helper; device handlers and state remain opt-in.
        services.AddScoped<DeviceGrantAdmission>();
        if (enabled)
        {
            services.AddSingleton<IDeviceGrantStateStore, MongoDeviceGrantStateStore>();
            services.AddScoped<DeviceGrantRateLimiter>();
            services.AddScoped<DeviceAuthorizationEndpoint>();
            services.AddScoped<DeviceVerificationEndpoint>();
            services.AddSingleton<DeviceGrantApproval>();
        }
    }

    /// <summary>Adds device handlers, replacing the corresponding OpenIddict defaults in order.</summary>
    /// <param name="server">The server builder.</param>
    internal static void AddDeviceHandlers(this OpenIddictServerBuilder server)
    {
        server.RemoveEventHandler(OpenIddictServerHandlers.AttachAuthorization.Descriptor)
            .AddEventHandler(AttachDeviceAuthorization.Descriptor)
            .RemoveEventHandler(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor)
            .AddEventHandler(RedeemDeviceCodeOnce.Descriptor)
            .AddEventHandler(BindDeviceAuthorization.Descriptor)
            .AddEventHandler(DeviceVerificationGuard.Descriptor)
            .AddEventHandler(DevicePollingAdmissionGuard.Descriptor)
            .AddEventHandler(DevicePollingGuard.Descriptor)
            .AddEventHandler(DeviceAuthorizationResponse.Descriptor)
            .AddEventHandler(DeviceVerificationResponse.Descriptor)
            .AddEventHandler(GuardDeviceIssuance.BeforeGeneration)
            .AddEventHandler(GuardDeviceIssuance.BeforeResponse);
    }
}
