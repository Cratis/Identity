// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Revokes a whole grant after its refresh token was reused, failing closed.
/// </summary>
/// <remarks>
/// The authorization is revoked first: a successor a concurrent winner creates after this point belongs to it and
/// cannot be refreshed or introspected on any replica. Only then are the grant's existing tokens revoked. Unlike
/// OpenIddict's own reuse handling, no error is swallowed: a request must never succeed without the revocation.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="authorizations">The <see cref="IOpenIddictAuthorizationManager"/>.</param>
internal class GrantRevocation(IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations)
{
    const int RevocationAttempts = 3;

    /// <summary>
    /// Revokes the authorization and then all of its tokens.
    /// </summary>
    /// <param name="authorizationId">The authorization identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    /// <exception cref="AuthorizationRevocationFailed">The authorization could not be revoked.</exception>
    public async Task RevokeAfterReuse(string authorizationId, CancellationToken cancellationToken)
    {
        // A false return is an optimistic-concurrency loss; refetch and retry.
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
