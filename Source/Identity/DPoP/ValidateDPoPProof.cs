// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.DPoP;

/// <summary>Validates new authorization-code and device-code bindings before stock redemption.</summary>
/// <param name="admission">The shared admission service.</param>
internal sealed class ValidateDPoPProof(DPoPAdmission admission) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor immediately before refresh admission and stock redemption.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<ValidateDPoPProof>()
        .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order - 2)
        .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType == OpenIddictServerEndpointType.Token && (context.Request.IsAuthorizationCodeGrantType() || context.Request.IsDeviceCodeGrantType()))
        {
            await admission.Admit(context);
        }
    }
}
