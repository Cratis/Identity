// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts;

/// <summary>
/// The link a credential was authenticated through: found at sign-in by upstream key, carried as a private claim, and
/// checked against the account's current links wherever the credential is used.
/// </summary>
internal static class AuthenticatingLink
{
    /// <summary>The private claim carrying the authenticating link; it is never given a token destination, so OpenIddict keeps it only in protected codes and refresh tokens (proved by spec); the session cookie carries it too.</summary>
    public const string ClaimType = "cratis_authenticating_link";

    /// <summary>Finds the link a verified authentication signed in through, by its verified upstream key and never by a mutable login or email.</summary>
    /// <param name="account">The active account after sign-in.</param>
    /// <param name="authentication">The verified upstream authentication.</param>
    /// <returns>The link, or null when the account has no link for the verified key.</returns>
    public static Guid? Find(Account account, VerifiedAuthentication authentication) =>
        account.LinkedUpstreamKeys.FirstOrDefault(key => key.IdpIssuer == authentication.IdpIssuer && key.IdpSub == authentication.Subject)?.LinkId.Value;

    /// <summary>Reads the authenticating link of a principal.</summary>
    /// <param name="principal">A session or grant principal.</param>
    /// <returns>The link, or null when the principal carries none or an unreadable value.</returns>
    public static Guid? Of(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(ClaimType)?.Value, out var link) && link != Guid.Empty ? link : null;

    /// <summary>Adds the authenticating link to an identity when one is known.</summary>
    /// <param name="identity">The identity to stamp.</param>
    /// <param name="link">The link, or null to leave the identity unstamped (it then fails closed on use).</param>
    public static void Stamp(ClaimsIdentity identity, Guid? link)
    {
        if (link is { } value)
        {
            identity.AddClaim(new Claim(ClaimType, value.ToString("D")));
        }
    }

    /// <summary>Checks that a link is still one of the account's current links.</summary>
    /// <param name="account">The current active account.</param>
    /// <param name="link">The recorded authenticating link.</param>
    /// <returns>Whether the link is known and current; a missing link is never current.</returns>
    public static bool IsCurrent(Account account, Guid? link) =>
        link is { } value && account.LinkedUpstreamKeys.Any(key => key.LinkId.Value == value);

    /// <summary>Finds the unique current GitHub link selected by the credential's protected stamp.</summary>
    /// <param name="identity">The stamped credential identity.</param>
    /// <param name="account">The current active account.</param>
    /// <returns>The supported GitHub link, or null for a missing, ambiguous or unsupported link.</returns>
    internal static LinkedUpstreamKey? SupportedGitHubLink(ClaimsIdentity identity, Account account)
    {
        if (Of(new ClaimsPrincipal(identity)) is not { } linkId)
        {
            return null;
        }

        var links = account.LinkedUpstreamKeys.Where(_ => _.LinkId.Value == linkId).Take(2).ToArray();
        return links is [var link] &&
            link.ConnectionId == GitHubConnection.FixedConnectionId &&
            link.IdpIssuer.Value == GitHubConnection.Issuer ? link : null;
    }
}
