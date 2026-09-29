// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Revokes the whole grant, authorization included, when a refresh token that is no longer valid (typically one
/// already redeemed) is presented, and fails closed when it cannot.
/// </summary>
/// <remarks>
/// OpenIddict 7.7.1's built-in <c language="csharp">ValidateTokenEntry</c> handler detects the reuse but only
/// revokes the tokens that exist at that moment, leaves the authorization valid and swallows revocation errors. A
/// winning request that redeemed the token moments earlier and has not yet stored its successor would then hand out
/// a working successor. This handler runs just before it and revokes the authorization first, so that successor
/// fails validation too. Reuse detected at redemption time is handled by <see cref="RedeemRefreshTokenOnce"/>.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="grants">The <see cref="GrantRevocation"/>.</param>
public class RevokeGrantOnRefreshTokenReuse(IOpenIddictTokenManager tokens, GrantRevocation grants) : IOpenIddictServerHandler<ValidateTokenContext>
{
    /// <summary>
    /// Gets the descriptor. It carries the same filters as OpenIddict's token-entry validation and runs just before it.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireTokenStorageEnabled>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireTokenIdResolved>()
            .UseScopedHandler<RevokeGrantOnRefreshTokenReuse>()
            .SetOrder(OpenIddictServerHandlers.Protection.ValidateTokenEntry.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (context.Principal?.HasTokenType(TokenTypeIdentifiers.RefreshToken) != true ||
            string.IsNullOrEmpty(context.TokenId) ||
            string.IsNullOrEmpty(context.AuthorizationId) ||
            await tokens.FindByIdAsync(context.TokenId, context.CancellationToken) is not { } token ||
            await tokens.HasStatusAsync(token, Statuses.Valid, context.CancellationToken))
        {
            // Anything else is left to OpenIddict's own token-entry validation.
            return;
        }

        await grants.RevokeAfterReuse(context.AuthorizationId, context.CancellationToken);
        context.Reject(
            error: context.EndpointType is OpenIddictServerEndpointType.Token ? Errors.InvalidGrant : Errors.InvalidToken,
            description: "The refresh token is no longer valid.");
    }
}
