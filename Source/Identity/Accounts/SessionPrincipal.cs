// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using Cratis.Identity.Contracts;
using Cratis.Identity.Upstream;
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
    /// <param name="authTime">When the upstream authentication of this session was verified.</param>
    /// <returns>The session <see cref="ClaimsPrincipal"/>.</returns>
    public static ClaimsPrincipal For(Account account, string upstreamScheme, DateTimeOffset authTime)
    {
        var identity = new ClaimsIdentity(upstreamScheme, Claims.Name, Claims.Role);
        identity.AddClaim(new Claim(Claims.Subject, account.Id.Value));
        identity.AddClaim(new Claim("sid", Guid.NewGuid().ToString("N")));
        identity.AddClaim(new Claim(Claims.Name, account.Name.Value));
        if (account.LinkFor(GitHubConnection.Issuer) is { } gitHub)
        {
            identity.AddClaim(new Claim(IdentityClaims.GitHubId, gitHub.IdpSub.Value));
            identity.AddClaim(new Claim(IdentityClaims.GitHubLogin, gitHub.Login.Value));
        }

        if (account.DisplayLogin() is { } login)
        {
            identity.AddClaim(new Claim(Claims.PreferredUsername, login.Value));
        }

        identity.AddClaim(new Claim(Claims.AuthenticationTime, authTime.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Gets the upstream authentication time of a session principal.
    /// </summary>
    /// <param name="principal">The session <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The authentication time, or <see langword="null"/> if the principal carries none (such a session must sign in again).</returns>
    public static DateTimeOffset? AuthTime(ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirst(Claims.AuthenticationTime)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;

    /// <summary>
    /// Gets the account identifier of a session principal.
    /// </summary>
    /// <param name="principal">The session <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The account identifier, or <see langword="null"/> if the principal carries none.</returns>
    public static string? AccountId(ClaimsPrincipal principal) => principal.FindFirst(Claims.Subject)?.Value;

    /// <summary>Gets the opaque identifier of this browser session.</summary>
    /// <param name="principal">The session principal.</param>
    /// <returns>The session identifier, or null for a session created before session identifiers were introduced.</returns>
    public static string? SessionId(ClaimsPrincipal principal) => principal.FindFirst("sid")?.Value;
}
