// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Atomically redeems a refresh token before OpenIddict issues its successor, and fails closed when it cannot.
/// </summary>
/// <remarks>
/// OpenIddict 7.7.1 ignores a failed redemption of a refresh token in its built-in <c language="csharp">RedeemTokenEntry</c>
/// handler, so two requests that both passed token validation could each receive a successor. This handler
/// reloads the token entry, requires it to still be valid, and redeems it through the store's optimistic
/// concurrency check (a conditional replace in MongoDB), so at most one request across all replicas wins.
/// Every loser revokes the grant's authorization and all of its tokens and is refused with
/// <c language="text">invalid_grant</c>: a successor the winner creates later still belongs to the revoked authorization and
/// fails validation. Concurrent use of a refresh token is therefore treated exactly like reuse.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="authorizations">The <see cref="IOpenIddictAuthorizationManager"/>.</param>
public class RedeemRefreshTokenOnce(IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations) : IOpenIddictServerHandler<ProcessSignInContext>
{
    const int RevocationAttempts = 3;

    /// <summary>
    /// Gets the descriptor. It runs just before OpenIddict's redemption handler, whose later attempt to redeem the
    /// already redeemed token is harmless; this handler alone decides whether a refresh request may issue tokens.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<RedeemRefreshTokenOnce>()
            .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType is not OpenIddictServerEndpointType.Token || context.Request?.IsRefreshTokenGrantType() != true)
        {
            return;
        }

        var authentication = context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName!);
        var principal = authentication?.RefreshTokenPrincipal;
        var tokenId = principal?.GetTokenId();
        var authorizationId = principal?.GetAuthorizationId();
        if (string.IsNullOrEmpty(tokenId) || string.IsNullOrEmpty(authorizationId) ||
            await tokens.FindByIdAsync(tokenId, context.CancellationToken) is not { } token)
        {
            context.Reject(error: Errors.InvalidGrant, description: "The refresh token is no longer valid.");
            return;
        }

        // The entry is reloaded after validation: another request may have redeemed it in between. Redeeming an
        // entry that is no longer valid would succeed against its fresh concurrency token, so check it first.
        if (await tokens.HasStatusAsync(token, Statuses.Valid, context.CancellationToken) &&
            await tokens.TryRedeemAsync(token, context.CancellationToken))
        {
            return;
        }

        await RevokeGrant(authorizationId, context.CancellationToken);
        context.Reject(error: Errors.InvalidGrant, description: "The refresh token was already used.");
    }

    async Task RevokeGrant(string authorizationId, CancellationToken cancellationToken)
    {
        // The authorization first: a successor created by a winning request after this point belongs to it and
        // cannot be refreshed or introspected on any replica. A false return is an optimistic-concurrency loss;
        // refetch and retry, and never let the request succeed without the revocation.
        for (var attempt = 1; ; attempt++)
        {
            var authorization = await authorizations.FindByIdAsync(authorizationId, cancellationToken);
            if (authorization is null ||
                await authorizations.HasStatusAsync(authorization, Statuses.Revoked, cancellationToken) ||
                await authorizations.TryRevokeAsync(authorization, cancellationToken))
            {
                break;
            }

            if (attempt == RevocationAttempts)
            {
                throw new AuthorizationRevocationFailed(authorizationId);
            }
        }

        await tokens.RevokeByAuthorizationIdAsync(authorizationId, cancellationToken);
    }
}
