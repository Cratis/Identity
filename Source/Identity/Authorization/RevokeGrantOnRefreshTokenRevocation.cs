// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Extends RFC 7009 revocation: revoking a refresh token revokes the whole grant it belongs to (the
/// authorization and every token issued under it), so a CLI logout also ends every rotated sibling and makes
/// the grant's access tokens fail introspection.
/// </summary>
/// <remarks>
/// OpenIddict itself only revokes the presented token. Refresh-token reuse is handled by OpenIddict already: a
/// redeemed refresh token presented again revokes every token of its authorization.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="authorizations">The <see cref="IOpenIddictAuthorizationManager"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class RevokeGrantOnRefreshTokenRevocation(
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations,
    ILogger<RevokeGrantOnRefreshTokenRevocation> logger) : IOpenIddictServerHandler<HandleRevocationRequestContext>
{
    /// <summary>
    /// Gets the descriptor that places the handler after OpenIddict has revoked the presented token.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<HandleRevocationRequestContext>()
            .UseScopedHandler<RevokeGrantOnRefreshTokenRevocation>()
            .SetOrder(OpenIddictServerHandlers.Revocation.RevokeToken.Descriptor.Order + 1_000)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(HandleRevocationRequestContext context)
    {
        if (context.IsRejected || !context.GenericTokenPrincipal.HasTokenType(TokenTypeIdentifiers.RefreshToken))
        {
            return;
        }

        var authorizationId = context.GenericTokenPrincipal.GetAuthorizationId();
        if (string.IsNullOrEmpty(authorizationId))
        {
            return;
        }

        var count = await tokens.RevokeByAuthorizationIdAsync(authorizationId, context.CancellationToken);
        var authorization = await authorizations.FindByIdAsync(authorizationId, context.CancellationToken);
        if (authorization is not null)
        {
            await authorizations.TryRevokeAsync(authorization, context.CancellationToken);
        }

        logger.GrantRevoked(authorizationId, count);
    }
}
