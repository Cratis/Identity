// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>A validated, fresh existing-account session binding.</summary>
/// <param name="Account">The canonical immutable account subject.</param>
/// <param name="SessionId">The authenticated session reference.</param>
internal record AccountSession(AccountSubject Account, string SessionId)
{
    /// <summary>Checks the trusted authenticated session before accepting a link ceremony.</summary>
    /// <param name="principal">The authenticated account principal.</param>
    /// <param name="time">The server clock.</param>
    /// <returns>The validated session binding.</returns>
    internal static AccountSession Require(ClaimsPrincipal principal, TimeProvider time)
    {
        AccountOperationRefused.Require(principal.Identity?.IsAuthenticated == true, "existing_account_proof_required");
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        AccountOperationRefused.Require(AccountSubject.TryParse(raw, out var subject), "existing_account_proof_required");
        var sid = principal.FindFirstValue("sid");
        AccountOperationRefused.Require(!string.IsNullOrEmpty(sid), "existing_account_proof_required");
        var authTime = principal.FindFirstValue("auth_time");
        AccountOperationRefused.Require(long.TryParse(authTime, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds), "fresh_existing_account_proof_required");
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        AccountOperationRefused.Require(seconds <= now && now - seconds <= 300, "fresh_existing_account_proof_required");
        return new(subject!, sid!);
    }
}
