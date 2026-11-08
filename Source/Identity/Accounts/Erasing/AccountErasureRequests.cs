// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>Accepts an erasure request for a tombstoned account, once per erasure id.</summary>
/// <param name="log">The account stream.</param>
/// <param name="authorization">The who-may-request policy.</param>
/// <param name="time">The trusted clock.</param>
internal class AccountErasureRequests(IAccountErasureLog log, IErasureAuthorization authorization, TimeProvider time)
{
    /// <summary>Appends <see cref="AccountErasureRequested"/> unless this request was already accepted.</summary>
    /// <param name="requester">The authenticated requester.</param>
    /// <param name="account">The account to erase.</param>
    /// <param name="erasureId">The caller-chosen, retry-stable request reference.</param>
    /// <returns>True when a request fact was appended; false for an idempotent repeat.</returns>
    /// <exception cref="AccountOperationRefused">Not authorized, not revoked first, or another erasure exists.</exception>
    public async Task<bool> Request(ClaimsPrincipal requester, AccountSubject account, ErasureId erasureId)
    {
        AccountOperationRefused.Require(await authorization.IsAllowed(requester, account), "erasure_not_authorized");
        var state = await log.Read(account);
        AccountOperationRefused.Require(state.Tombstoned, "account_not_revoked");
        if (state.Requested == erasureId)
        {
            return false;
        }

        AccountOperationRefused.Require(state.Requested is null, "erasure_already_requested");
        await log.Append(account, new AccountErasureRequested(erasureId, time.GetUtcNow()));
        return true;
    }
}
