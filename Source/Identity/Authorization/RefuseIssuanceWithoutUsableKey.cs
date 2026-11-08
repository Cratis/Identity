// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Refuses to issue tokens, or redeem a grant for new ones, while no unexpired signing or encryption key is available.
/// </summary>
/// <remarks>
/// An expired certificate must not keep signing. The refusal runs before the refresh token is redeemed, so the grant
/// survives until a key is provisioned. JWKS keeps publishing the public keys that are still valid.
/// </remarks>
/// <param name="keys">The <see cref="SecretStoreKeys"/>.</param>
internal class RefuseIssuanceWithoutUsableKey(SecretStoreKeys keys) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>
    /// Gets the descriptor. It runs before any other sign-in handler.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<RefuseIssuanceWithoutUsableKey>()
            .SetOrder(OpenIddictServerHandlers.ValidateSignInDemand.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (!keys.CanIssue)
        {
            context.Reject(error: Errors.ServerError, description: "The server has no usable signing key.");
        }

        return ValueTask.CompletedTask;
    }
}
