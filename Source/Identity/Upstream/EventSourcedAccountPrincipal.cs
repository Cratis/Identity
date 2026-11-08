// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>Claim material for the new account path, never matching proof or an existing-session proof.</summary>
internal static class EventSourcedAccountPrincipal
{
    /// <summary>Creates claims with display-only preferred_username from the protected observed login.</summary>
    /// <param name="account">The immutable account subject.</param>
    /// <param name="login">The display-only identifier just observed for the authenticated key.</param>
    /// <param name="time">The authentication clock.</param>
    /// <returns>The new session principal.</returns>
    public static ClaimsPrincipal For(AccountSubject account, Login login, TimeProvider time)
    {
        var identity = new ClaimsIdentity("EventSourcedAccount");
        identity.AddClaim(new(ClaimTypes.NameIdentifier, account.Value));
        identity.AddClaim(new("sub", account.Value));
        identity.AddClaim(new("preferred_username", login.Value));
        identity.AddClaim(new("sid", Guid.NewGuid().ToString("D")));
        identity.AddClaim(new("auth_time", time.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        return new(identity);
    }
}
