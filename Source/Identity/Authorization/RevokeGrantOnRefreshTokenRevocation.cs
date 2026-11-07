// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
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
/// OpenIddict itself only revokes the presented token. Refresh-token reuse is guarded by
/// <see cref="RevokeGrantOnRefreshTokenReuse"/> at validation time and by <see cref="RedeemRefreshTokenOnce"/> at
/// redemption time, including concurrent reuse.
/// </remarks>
/// <param name="grants">The shared irreversible restriction protocol.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class RevokeGrantOnRefreshTokenRevocation(
    GrantRevocation grants,
    ILogger<RevokeGrantOnRefreshTokenRevocation> logger) : IOpenIddictServerHandler<HandleRevocationRequestContext>
{
    /// <summary>
    /// Gets the descriptor that commits the permanent fence before OpenIddict revokes the presented entry.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<HandleRevocationRequestContext>()
            .UseScopedHandler<RevokeGrantOnRefreshTokenRevocation>()
            .SetOrder(OpenIddictServerHandlers.Revocation.RevokeToken.Descriptor.Order - 1)
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

        var count = await grants.Revoke(authorizationId, RevocationReason.RefreshTokenRevoked, context.CancellationToken);

        logger.GrantRevoked(authorizationId, count);
    }
}
