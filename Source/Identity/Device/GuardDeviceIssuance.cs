// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Rechecks complete recovery admission before token generation and before returning credentials.</summary>
/// <param name="admission">The device authority.</param>
internal sealed class GuardDeviceIssuance(DeviceGrantAdmission admission) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor before credential generation.</summary>
    internal static OpenIddictServerHandlerDescriptor BeforeGeneration { get; } = Describe(OpenIddictServerHandlers.GenerateAccessToken.Descriptor.Order - 1);

    /// <summary>Gets the descriptor after token parameters are attached, before response serialization.</summary>
    internal static OpenIddictServerHandlerDescriptor BeforeResponse { get; } = Describe(OpenIddictServerHandlers.AttachSignInParameters.Descriptor.Order + 1);

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        var deviceRedemption = context.EndpointType == OpenIddictServerEndpointType.Token && context.Request?.IsDeviceCodeGrantType() == true;
        if (!deviceRedemption && context.EndpointType is not (OpenIddictServerEndpointType.DeviceAuthorization or OpenIddictServerEndpointType.EndUserVerification)) return;
        var authentication = context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName!);
        var allowed = deviceRedemption
            ? authentication?.DeviceCodePrincipal is { } principal && await admission.CanUse(principal, true, context.CancellationToken)
            : await admission.Ready(context.CancellationToken);
        if (!allowed) context.Reject(Errors.TemporarilyUnavailable, "Device authorization is unavailable.");
    }

    static OpenIddictServerHandlerDescriptor Describe(int order) => OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<GuardDeviceIssuance>().SetOrder(order).SetType(OpenIddictServerHandlerType.Custom).Build();
}
