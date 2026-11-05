// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking;

internal record AccountSession(AccountSubject Account, string SessionId)
{
    internal static AccountSession Require(ClaimsPrincipal principal, TimeProvider time)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var sid = principal.FindFirstValue("sid");
        var authTime = principal.FindFirstValue("auth_time");
        if (principal.Identity?.IsAuthenticated != true || !AccountSubject.TryParse(raw, out var subject) || string.IsNullOrEmpty(sid))
        {
            throw new AccountOperationRefused("existing_account_proof_required");
        }
        if (!long.TryParse(authTime, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            throw new AccountOperationRefused("fresh_existing_account_proof_required");
        }
        var now = time.GetUtcNow().ToUnixTimeSeconds();
        if (seconds > now || now - seconds > 300)
        {
            throw new AccountOperationRefused("fresh_existing_account_proof_required");
        }
        return new(subject!, sid);
    }
}
