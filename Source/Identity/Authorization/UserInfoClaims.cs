// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Projects current canonical account data into the bounded UserInfo response.</summary>
internal static class UserInfoClaims
{
    /// <summary>Creates a response from an active account and its granted profile scope.</summary>
    /// <param name="account">The current active canonical account.</param>
    /// <param name="hasProfile">Whether profile was actually granted.</param>
    /// <returns>Only sub, and the two permitted profile claims when granted.</returns>
    public static Dictionary<string, object> For(Account account, bool hasProfile)
    {
        Dictionary<string, object> claims = new(StringComparer.Ordinal) { [Claims.Subject] = account.Id.Value };
        if (hasProfile)
        {
            claims[Claims.Name] = account.Name.Value;
            if (account.DisplayLogin() is { } login)
            {
                claims[Claims.PreferredUsername] = login.Value;
            }
        }

        return claims;
    }
}
