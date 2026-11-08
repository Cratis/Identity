// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Revokes a whole grant after its refresh token was reused, failing closed.
/// </summary>
/// <remarks>
/// The irreversible grant fence and outbox commit before log confirmation and OpenIddict hygiene. Denial does
/// not wait for Chronicle, a projection, a worker, or token pruning. Offline access tokens retain the accepted
/// 600-second lifetime plus 30-second skew (630-second residual window); this protocol does not rewrite JWTs.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="authorizations">The <see cref="IOpenIddictAuthorizationManager"/>.</param>
/// <param name="fences">The authoritative grant documents.</param>
/// <param name="confirm">The durable fence-log confirmation.</param>
internal class GrantRevocation(IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations, IFenceStore fences, ConfirmFenceLog confirm)
{
    const int RevocationAttempts = 3;

    /// <summary>
    /// Revokes the authorization and then all of its tokens.
    /// </summary>
    /// <param name="authorizationId">The authorization identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>An awaitable <see cref="Task"/>.</returns>
    /// <exception cref="AuthorizationRevocationFailed">The authorization could not be revoked.</exception>
    public Task RevokeAfterReuse(string authorizationId, CancellationToken cancellationToken) => Revoke(authorizationId, RevocationReason.RefreshTokenReused, cancellationToken);

    /// <summary>Commits denial before confirmation and best-effort store hygiene; failures never acknowledge success.</summary>
    /// <param name="authorizationId">The authorization.</param>
    /// <param name="reason">The closed restriction reason.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The completed acknowledgement.</returns>
    /// <exception cref="RevocationPending">The restriction could not be confirmed.</exception>
    /// <exception cref="AuthorizationRevocationFailed">The authorization cleanup could not be confirmed.</exception>
    internal async Task<long> Revoke(string authorizationId, RevocationReason reason, CancellationToken cancellationToken)
    {
        var state = await fences.Read(authorizationId, cancellationToken);
        var subject = state?.Fence?.Subject ?? state?.Outbox.FirstOrDefault()?.Subject;
        if (!AccountSubject.TryParse(subject, out _))
        {
            var authorization = await authorizations.FindByIdAsync(authorizationId, cancellationToken);
            subject = authorization is null ? null : await authorizations.GetSubjectAsync(authorization, cancellationToken);
        }

        if (!AccountSubject.TryParse(subject, out _))
        {
            throw new RevocationPending();
        }

        var fence = await fences.Commit(authorizationId, subject!, reason, cancellationToken);

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

        var count = await tokens.RevokeByAuthorizationIdAsync(authorizationId, cancellationToken);
        await confirm.Confirm(fence, cancellationToken);
        return count;
    }
}
