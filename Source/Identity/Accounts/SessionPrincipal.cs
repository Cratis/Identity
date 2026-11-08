// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Creates the principal stored in the Identity site session cookie.
/// </summary>
internal static class SessionPrincipal
{
    /// <summary>
    /// Creates the session principal for an account.
    /// </summary>
    /// <param name="account">The signed-in <see cref="Account"/>.</param>
    /// <param name="upstreamScheme">The upstream scheme the account signed in through.</param>
    /// <returns>The session <see cref="ClaimsPrincipal"/>.</returns>
    public static ClaimsPrincipal For(Account account, string upstreamScheme)
    {
        var identity = new ClaimsIdentity(upstreamScheme, Claims.Name, Claims.Role);
        identity.AddClaim(new Claim(Claims.Subject, account.Id.Value));
        identity.AddClaim(new Claim(Claims.Name, account.Name.Value));
        identity.AddClaim(new Claim(IdentityClaims.GitHubId, account.GitHubId.Value));
        identity.AddClaim(new Claim(IdentityClaims.GitHubLogin, account.GitHubLogin.Value));
        identity.AddClaim(new Claim(Claims.PreferredUsername, account.GitHubLogin.Value));
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Gets the account identifier of a session principal.
    /// </summary>
    /// <param name="principal">The session <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The account identifier, or <see langword="null"/> if the principal carries none.</returns>
    public static string? AccountId(ClaimsPrincipal principal) => principal.FindFirst(Claims.Subject)?.Value;
}
