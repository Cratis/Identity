// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Preserves stock redemption for other flows and permanently fences a reused device grant.</summary>
/// <param name="tokens">The token manager.</param>
/// <param name="grants">The existing durable restriction protocol.</param>
/// <param name="admission">The device recovery and authority checks.</param>
/// <param name="time">The clock.</param>
internal sealed class RedeemDeviceCodeOnce(IOpenIddictTokenManager tokens, GrantRevocation grants, DeviceGrantAdmission admission, TimeProvider time) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the replacement descriptor at the stock redemption boundary.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireTokenStorageEnabled>()
        .UseScopedHandler<RedeemDeviceCodeOnce>()
        .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.Token || context.Request?.IsDeviceCodeGrantType() != true)
        {
            await new OpenIddictServerHandlers.RedeemTokenEntry(tokens).HandleAsync(context);
            return;
        }

        var principal = context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName!)?.DeviceCodePrincipal;
        if (principal is null || !await admission.CanUse(principal, true, context.CancellationToken))
        {
            context.Reject(Errors.InvalidGrant, "The device authorization is no longer valid.");
            return;
        }

        var id = principal.GetTokenId();
        var token = string.IsNullOrEmpty(id) ? null : await tokens.FindByIdAsync(id, context.CancellationToken);
        if (token is null || await tokens.GetExpirationDateAsync(token, context.CancellationToken) is not { } expires || expires <= time.GetUtcNow())
        {
            context.Reject(Errors.ExpiredToken, "The device authorization has expired.");
            return;
        }

        if (!await tokens.TryRedeemAsync(token, context.CancellationToken))
        {
            await grants.Revoke(principal.GetAuthorizationId()!, RevocationReason.Revoked, context.CancellationToken);
            context.Reject(Errors.InvalidGrant, "The device authorization is no longer valid.");
        }
    }
}
