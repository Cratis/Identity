// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Hands the link a new grant authenticated through to the refresh token insert, so the family starts with it.
/// Rotation never runs through here: a successor inherits the link from the admitted family state.
/// </summary>
/// <param name="issuance">The request-scoped <see cref="RefreshIssuance"/>.</param>
internal sealed class CaptureAuthenticatingLink(RefreshIssuance issuance) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>
    /// Gets the descriptor. It runs before any token is generated.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<CaptureAuthenticatingLink>()
            .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order + 3)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is { } principal)
        {
            issuance.StartLink = AuthenticatingLink.Of(principal);
        }

        return ValueTask.CompletedTask;
    }
}
