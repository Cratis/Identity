// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Never places a user code in a URL, browser history, or a referrer.</summary>
/// <param name="options">The advertised polling interval.</param>
internal sealed class DeviceAuthorizationResponse(IOptions<IdentityOptions> options) : IOpenIddictServerHandler<ApplyDeviceAuthorizationResponseContext>
{
    /// <summary>Gets the response descriptor before host response serialization.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ApplyDeviceAuthorizationResponseContext>()
        .UseScopedHandler<DeviceAuthorizationResponse>().SetOrder(int.MinValue + 100_000)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ApplyDeviceAuthorizationResponseContext context)
    {
        context.Response[Parameters.VerificationUriComplete] = null;
        if (string.IsNullOrEmpty(context.Response.Error)) context.Response[Parameters.Interval] = (long)options.Value.DeviceGrant.PollingInterval.TotalSeconds;
        return ValueTask.CompletedTask;
    }
}
