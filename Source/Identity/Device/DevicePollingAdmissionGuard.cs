// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Checks device polling availability before code validation.</summary>
/// <param name="admission">The recovery authority.</param>
/// <param name="options">The device flow switch.</param>
internal sealed class DevicePollingAdmissionGuard(DeviceGrantAdmission admission, IOptions<IdentityOptions> options) : IOpenIddictServerHandler<ProcessAuthenticationContext>
{
    /// <summary>Gets the descriptor before device-code validation.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
        .UseScopedHandler<DevicePollingAdmissionGuard>()
        .SetOrder(OpenIddictServerHandlers.ValidateDeviceCode.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.Token || !context.Request.IsDeviceCodeGrantType()) return;
        if (!options.Value.DeviceGrant.Enabled || !await admission.Ready(context.CancellationToken))
        {
            context.Reject(Errors.TemporarilyUnavailable, "Device authorization is unavailable.");
        }
    }
}
