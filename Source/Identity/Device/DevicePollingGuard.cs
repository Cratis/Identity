// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Enforces persistent RFC 8628 polling intervals after code validation.</summary>
/// <param name="rates">The shared polling windows.</param>
internal sealed class DevicePollingGuard(DeviceGrantRateLimiter rates) : IOpenIddictServerHandler<ProcessAuthenticationContext>
{
    /// <summary>Gets the descriptor after device-code validation and before sign-in redemption.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
        .UseScopedHandler<DevicePollingGuard>()
        .SetOrder(OpenIddictServerHandlers.ValidateDeviceCode.Descriptor.Order + 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        if (!CanPoll(context)) return;
        var id = context.DeviceCodePrincipal!.GetTokenId();
        if (string.IsNullOrEmpty(id)) return;
        var slowed = await rates.Poll(id, context.CancellationToken);
        if (slowed is null) context.Reject(Errors.TemporarilyUnavailable, "Device authorization is unavailable.");
        else if (slowed.Value) context.Reject(Errors.SlowDown, "The polling interval has increased by five seconds.");
    }

    /// <summary>Checks the presenter because OpenIddict defers that validation to Exchange.ValidateAuthorizedParty.</summary>
    /// <param name="context">The authentication context containing the validated device code.</param>
    /// <returns>Whether the device code belongs to this client and can be polled.</returns>
    static bool CanPoll(ProcessAuthenticationContext context) =>
        context.EndpointType == OpenIddictServerEndpointType.Token && context.Request.IsDeviceCodeGrantType() &&
        !context.IsRejected && context.DeviceCodePrincipal is { } principal &&
        !string.IsNullOrEmpty(context.ClientId) && principal.HasPresenter(context.ClientId);
}
