// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Authorization;

/// <summary>Rechecks authoritative refresh state without consuming an entry or weakening reuse fencing.</summary>
/// <param name="fences">The primary grant authority.</param>
internal class RefreshGrantAdmission(IFenceStore fences)
{
    /// <summary>Fails closed when the grant state cannot be read or no longer admits redemption.</summary>
    /// <param name="granted">The validated token principal.</param>
    /// <param name="request">The validated token request.</param>
    /// <param name="account">The current active account; a refresh must have authenticated through one of its current links.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>Whether redemption may reach the existing consume/install operation.</returns>
    public async Task<bool> IsValid(ClaimsPrincipal granted, OpenIddictRequest request, Account account, CancellationToken cancellationToken)
    {
        var authorizationId = granted.GetAuthorizationId();
        if (string.IsNullOrEmpty(authorizationId)) return false;
        try
        {
            var state = await fences.Read(authorizationId, cancellationToken);
            if (!Admits(state, request)) return false;
            if (!NeedsLinkCheck(state, request) || AuthenticatingLink.IsCurrent(account, state?.LinkId)) return true;

            // The link is unlinked or was never recorded: the family can never refresh again. This is the lazy enforcement on use, in addition to the unlink-time fence.
            await fences.Commit(authorizationId, account.Id.Value, RevocationReason.Revoked, cancellationToken);
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool NeedsLinkCheck(GrantState? state, OpenIddictRequest request) => state is not null && request.IsRefreshTokenGrantType();

    static bool Admits(GrantState? state, OpenIddictRequest request)
    {
        if (state is null) return !request.IsRefreshTokenGrantType();

        // A known non-current entry must reach redemption so reuse fences the family.
        return state.Fence is null && GrantStateValidation.IsKnown(state);
    }
}
