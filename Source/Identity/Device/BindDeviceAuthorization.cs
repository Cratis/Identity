// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Attaches the approved authorization to the existing device entry without replacing its epoch properties.</summary>
/// <param name="tokens">The token manager.</param>
/// <param name="admission">The readiness check.</param>
internal sealed class BindDeviceAuthorization(IOpenIddictTokenManager tokens, DeviceGrantAdmission admission) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor, immediately after stock device payload replacement.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<BindDeviceAuthorization>()
        .SetOrder(OpenIddictServerHandlers.UpdateReferenceDeviceCodeEntry.Descriptor.Order + 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.EndUserVerification) return;
        var id = context.DeviceCodePrincipal?.GetTokenId();
        var token = string.IsNullOrEmpty(id) ? null : await tokens.FindByIdAsync(id, context.CancellationToken);
        if (token is null || !await admission.Ready(context.CancellationToken))
        {
            context.Reject(Errors.InvalidGrant, "The device authorization is no longer valid.");
            return;
        }

        var descriptor = new OpenIddictTokenDescriptor();
        await tokens.PopulateAsync(descriptor, token, context.CancellationToken);
        descriptor.AuthorizationId = context.Principal!.GetAuthorizationId();
        await tokens.UpdateAsync(token, descriptor, context.CancellationToken);
    }
}
